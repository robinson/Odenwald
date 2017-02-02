using log4net;
using Odenwald;
using Odenwald.Common.Opc;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.OpcUaPlugin
{
    public class OpcUaPlugin : IMetricsReadPlugin, IDisposable
    {
        //TODO: 1. check last opc status 
        #region attributes
        static ILog l_logger = LogManager.GetLogger(typeof(OpcUaPlugin));
        //string l_opcuaUrl;
        //ApplicationConfiguration l_applicationConfig;
        OpcPluginConfig l_opcuaPluginConfig;
        Session l_session;
        //Subscription l_subscription;
        BlockingCollection<OpcMetric> l_metricCollection;
        List<PolledMeasurement> PolledMeasurements;
        List<MonitoredMeasurement> MonitoredMeasurements;
        static Dictionary<string, object> l_cache = new Dictionary<string, object>();
        #endregion

        #region imetricsreadplugin methods
        public void Configure()
        {
            l_opcuaPluginConfig = OpcPluginConfig.GetConfig("OpcUa");// ConfigurationManager.GetSection("OpcUa") as OpcPluginConfig;
            if (l_opcuaPluginConfig == null)
            {
                throw new Exception("Cannot get configuration section : OpcUa");
            }
            
            //l_opcuaUrl = l_opcuaPluginConfig.Settings.Url;
            //lth: this connect is not safe.
            //l_applicationConfig = new ApplicationConfiguration()
            //{
            //    ApplicationName = "Odenwald",
            //    ApplicationType = ApplicationType.Client,
            //    SecurityConfiguration = new SecurityConfiguration
            //    {
            //        ApplicationCertificate = new CertificateIdentifier
            //        {
            //            StoreType = @"Windows",
            //            StorePath = @"CurrentUser\My",
            //            SubjectName = Utils.Format(@"CN={0}, DC={1}",
            //           "Odenwald",
            //           System.Net.Dns.GetHostName())
            //        },
            //        TrustedPeerCertificates = new CertificateTrustList
            //        {
            //            StoreType = @"Windows",
            //            StorePath = @"CurrentUser\TrustedPeople",
            //        },
            //        NonceLength = 32,
            //        AutoAcceptUntrustedCertificates = true
            //    },
            //    TransportConfigurations = new TransportConfigurationCollection(),
            //    TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            //    ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
            //};
            //l_applicationConfig.Validate(ApplicationType.Client);
            //if (l_applicationConfig.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            //{
            //    l_applicationConfig.CertificateValidator.CertificateValidation += (s, e) =>
            //    { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
            //}


        }
       
        public void Start()
        {
            InitializeMeasurements();
            l_session = InitializeSession(new Uri(l_opcuaPluginConfig.Settings.Url));
            //lth: create session is not safe
            //l_session = Session.Create(l_applicationConfig, new ConfiguredEndpoint(null, new EndpointDescription(l_opcuaUrl)), true, "Odenwald", 60000, null, null);//EndpointDescription need to be changed according to your OPC server
            //l_subscription = new Subscription(l_session.DefaultSubscription) { PublishingInterval = 1000 };

            l_metricCollection = new BlockingCollection<OpcMetric>();
            StartMonitoring();
            StartPolling();
        }
       

        public void Stop()
        {
            //unsubscribe and close session
            if (l_session != null)
            {
                for (int i = 0; i < l_session.Subscriptions.Count(); i++)
                {
                    var sub = l_session.Subscriptions.ToList()[i];
                    Unsubscribe(sub);
                }
                l_session.RemoveSubscriptions(l_session.Subscriptions.ToList());
                l_session.Close();
                l_session.Dispose();
            }
        }
        void Unsubscribe(Subscription sub)
        {
            sub.RemoveItems(sub.MonitoredItems);
            sub.Delete(true);
            l_session.RemoveSubscription(sub);
            sub.Dispose();
        }

        public IList<MetricValue> Read()
        {
            if (l_metricCollection.Count <= 0)
                return null;
            List<MetricValue> metricList = new List<MetricValue>();

            while (l_metricCollection.Count > 0)
            {

                OpcMetric data = null;
                try
                {
                    data = l_metricCollection.Take();
                }
                catch (InvalidOperationException) { }

                if (data != null)
                {
                    MetricValue metricValue = new MetricValue()
                    {
                        TypeName = "gauge",
                        TypeInstanceName = "gauge",
                        //Interval = data.Measurement.mo
                        HostName = "localhost",
                        PluginInstanceName = "OpcUaPluginInstance",
                        PluginName = "OpcUaPlugin",
                        Extension = data.JsonString()
                    };
                    metricList.Add(metricValue);
                }
            }
            l_logger.DebugFormat("Read {0} items.", metricList.Count);
            return metricList;

        }
        #endregion

        #region helper
        bool QualifyMetric(ref OpcMetric metric)
        {
            if (PointHasGoodOrDifferentBadStatus(metric))
            {
                if (!l_cache.ContainsKey(metric.Measurement.Name))
                {
                    l_cache.Add(metric.Measurement.Name, metric.OpcValue);
                    metric.Measurement.LastOpcstatus = metric.Opcstatus;
                    metric.Measurement.LastValue = metric.OpcValue;
                }
                else
                {
                    object lastvalue;
                    l_cache.TryGetValue(metric.Measurement.Name, out lastvalue);
                    metric.Measurement.LastValue = lastvalue;
                }


                if (!PointIsValid(metric) || !PointMatchesType(metric))
                {
                    // Set de default value for the type specified
                    l_logger.ErrorFormat("Invalid point: measurement.name<{0}>, measurement.path<{1}>, metric.value<{2}>", metric.Measurement.Name, metric.Measurement.Path, metric.OpcValue.ToString());
                    switch (metric.Measurement.DataType)
                    {
                        case "number":
                            metric.OpcValue = 0;
                            break;
                        case "boolean":
                            metric.OpcValue = false;
                            break;
                        case "string":
                            metric.OpcValue = "";
                            break;
                        default:
                            l_logger.Error("No valid datatype, ignoring point");
                            break;
                    }
                    return false;
                }
            }
            // Check for deadband
            if (PointIsWithinDeadband(metric)) return false;
            if (!PointMatchesType(metric))
            {
                l_logger.ErrorFormat("Invalid type returned from OPC. Ignoring point {0}", metric.Measurement.Name);
                return false;
            }
            return true;
        }
        bool PointMatchesType(OpcMetric metric)
        {
            var match = (metric.OpcValue.IsNumber() && metric.Measurement.DataType == "number")
                || metric.OpcValue.IsBoolean() && metric.Measurement.DataType == "boolean"
                || metric.OpcValue.IsString() && metric.Measurement.DataType == "string";

            if (!match)
            {
                //log here

            }
            return match;
        }
        bool PointIsValid(OpcMetric p)
        {
            // check if the value is a type that we can handle (number or a bool).
            return (p.OpcValue != null && (p.OpcValue.IsBoolean() || p.OpcValue.IsNumber())) || p.OpcValue.IsString();

        }
        bool PointHasGoodOrDifferentBadStatus(OpcMetric p)
        {
            var curr = p.Opcstatus;
            var prev = p.Measurement.LastOpcstatus;

            if (curr == "Good" || curr != prev) return true;
            return false;
        }


        bool PointIsWithinDeadband(OpcMetric metric)
        {
            // some vars for shorter statements later on.
            var curr = metric.OpcValue;
            var prev = metric.Measurement.LastValue;
            var dba = metric.Measurement.DeadbandAbsolute;
            var dbr = metric.Measurement.DeadbandRelative;

            // return early if the type of the previous value is not the same as the current.
            // this will also return when this is the first value and prev is still undefined.
            if (curr.GetType() != prev.GetType()) return false;

            // calculate deadbands based on value type. For numbers, make the
            // calculations for both absolute and relative if they are set. For bool,
            // just check if a deadband has been set and if the value has changed.

            if (curr.IsNumber())
            {

                if (dba > 0 && Math.Abs(Convert.ToDecimal(curr) - Convert.ToDecimal(prev)) < dba)
                {
                    return true;
                }
                if (dbr > 0 && Math.Abs(Convert.ToDecimal(curr) - Convert.ToDecimal(prev)) < Math.Abs(Convert.ToDecimal(prev)) * dbr)
                {
                    // console.log("New value is within relative deadband.", p);
                    return true;

                }
            }
            else if (curr.IsBoolean())
            {
                if (dba > 0 && prev == curr)
                    // console.log("New value is within bool deadband.", p);
                    return true;

            }
            else if (curr.IsString())
                return true;
            return false;

        }
        #endregion

        #region methods
        private Session InitializeSession(Uri url, X509Certificate2 applicationCertificate = null )
        {
            var certificateValidator = new CertificateValidator();
            certificateValidator.CertificateValidation += (sender, eventArgs) =>
            {
                if (ServiceResult.IsGood(eventArgs.Error))
                    eventArgs.Accept = true;
                else if ((eventArgs.Error.StatusCode.Code == StatusCodes.BadCertificateUntrusted) && true) // AutoAcceptUntrustedCertificates = true;
                    eventArgs.Accept = true;
                else
                    throw new Exception(string.Format("Failed to validate certificate with error code {0}: {1}, statusCode: {2}", eventArgs.Error.Code, eventArgs.Error.AdditionalInfo, eventArgs.Error.StatusCode));
            };
            // Build the application configuration
            var appInstance = new ApplicationInstance
            {
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = "Odenwald",
                ApplicationConfiguration = new ApplicationConfiguration
                {
                    ApplicationUri = url.ToString(),
                    ApplicationName = "Odenwald",
                    ApplicationType = ApplicationType.Client,
                    CertificateValidator = certificateValidator,
                    ServerConfiguration = new ServerConfiguration
                    {
                        MaxSubscriptionCount = 100,
                        MaxMessageQueueSize = 10,
                        MaxNotificationQueueSize = 100,
                        MaxPublishRequestCount = 20
                    },
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        AutoAcceptUntrustedCertificates = true
                    },
                    TransportQuotas = new TransportQuotas
                    {
                        OperationTimeout = 600000,
                        MaxStringLength = 1048576,
                        MaxByteStringLength = 1048576,
                        MaxArrayLength = 65535,
                        MaxMessageSize = 4194304,
                        MaxBufferSize = 65535,
                        ChannelLifetime = 600000,
                        SecurityTokenLifetime = 3600000
                    },
                    ClientConfiguration = new ClientConfiguration
                    {
                        DefaultSessionTimeout = 60000,
                        MinSubscriptionLifetime = 10000
                    },
                    DisableHiResClock = true
                }
            };

            // Assign a application certificate (when specified)
            if (applicationCertificate != null)
                appInstance.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate = new CertificateIdentifier(applicationCertificate);

            // Find the endpoint to be used
            var endpoints = OpcUaHelper.SelectEndpoint(url, false);//UseMessageSecurity = false;

            // Create the OPC session:
            var session = Session.Create(
                configuration: appInstance.ApplicationConfiguration,
                endpoint: new ConfiguredEndpoint(
                    collection: null,
                    description: endpoints,
                    configuration: EndpointConfiguration.Create(applicationConfiguration: appInstance.ApplicationConfiguration)),
                updateBeforeConnect: false,
                checkDomain: false,
                sessionName: "Odenwald",
                sessionTimeout: 60000U,
                identity: null,
                preferredLocales: new string[] { });

            return session;
        }
        void InitializeMeasurements()
        {
            MonitoredMeasurements = new List<MonitoredMeasurement>();
            PolledMeasurements = new List<PolledMeasurement>();
            foreach (MeasurementConfig item in l_opcuaPluginConfig.Measurements)
            {
                Dictionary<string, string> tags = null;
                if (!item.Tags.Equals(""))
                    tags = item.Tags.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(part => part.Split(':'))
                                .ToDictionary(split => split[0], split => split[1]);
                switch (item.CollectionType)
                {
                    case "monitored":

                        var monitoredpoint = new MonitoredMeasurement()
                        {
                            Name = item.Name,
                            Path = item.Path,
                            MonitorResolution = item.MonitorResolution > 0 ? item.MonitorResolution : 0,
                            DeadbandAbsolute = item.DeadbandAbsolute > 0 ? item.DeadbandAbsolute : 0,
                            DeadbandRelative = item.DeadbandRelative > 0 ? item.DeadbandRelative : 0,
                            DataType = item.DataType,
                            Tags = tags

                        };

                        MonitoredMeasurements.Add(monitoredpoint);
                        break;
                    case "polled":
                        var polledPoint = new PolledMeasurement()
                        {
                            Name = item.Name,
                            Path = item.Path,
                            DeadbandAbsolute = item.DeadbandAbsolute > 0 ? item.DeadbandAbsolute : 0,
                            DeadbandRelative = item.DeadbandRelative > 0 ? item.DeadbandRelative : 0,
                            PollInterval = item.PollInterval,
                            DataType = item.DataType,
                            Tags = tags
                        };

                        PolledMeasurements.Add(polledPoint);

                        break;
                    default:
                        //no collection type, log 
                        l_logger.DebugFormat("no collection type at: {0}", item.Name);
                        break;
                }
            }
        }
        void StartMonitoring()
        {
            foreach (var item in MonitoredMeasurements)
            {
                var nodeId = OpcUaHelper.FindNode(item.Path, ObjectIds.ObjectsFolder, l_session);
                var sub = new Subscription
                {
                    PublishingInterval = item.MonitorResolution,
                    PublishingEnabled = true,
                    LifetimeCount = 0,
                    KeepAliveCount = 0,
                    DisplayName = item.Path,
                    Priority = byte.MaxValue
                };
                MonitoredItem monitoredItem = new MonitoredItem()
                {
                    StartNodeId = nodeId,
                    AttributeId = Attributes.Value,
                    DisplayName = item.Name,
                    SamplingInterval = item.MonitorResolution
                };
               
                sub.AddItem(monitoredItem);
                l_session.AddSubscription(sub);

                sub.Create();
                sub.ApplyChanges();
                monitoredItem.Notification += (monitored, args) =>
                {
                    var p = (MonitoredItemNotification)args.NotificationValue;
                    MonitorData(item, p.Value);
                };
            }

        }
        void MonitorData(MonitoredMeasurement measurement, DataValue NewValue)
        {
            OpcMetric metric = new OpcMetric();
            metric.Timestamp = NewValue.SourceTimestamp;
            metric.OpcValue = NewValue.WrappedValue.Value;
            metric.Opcstatus = NewValue.StatusCode.ToString();
            metric.Measurement = (MeasurementDto)measurement;

            l_metricCollection.Add(metric);
        }

        void StartPolling()
        {
            foreach (var item in PolledMeasurements)
            {
                var task = Task.Factory.StartNew((Func<Task>)(async () =>  // <- marked async
                {
                    do
                    {
                        ReadMeasurement(item);
                        await Task.Delay(TimeSpan.FromMilliseconds(item.PollInterval));
                    } while (true);
                }));
            }
        }
        void ReadMeasurement(PolledMeasurement measurement)
        {
            OpcMetric metric = new OpcMetric();

            metric.Measurement = (MeasurementDto)measurement;
            //check server is connected??
            var nodesToRead = OpcUaHelper.GetReadValueIdCollection(measurement.Path, l_session);
            DataValueCollection results;
            DiagnosticInfoCollection diag;
            l_session.Read(
                requestHeader: null,
                maxAge: 0,
                timestampsToReturn: TimestampsToReturn.Neither,
                nodesToRead: nodesToRead,
                results: out results,
                diagnosticInfos: out diag);
            var val = results[0];
            //metric.Measurement.LastOpcstatus = val.StatusCode.ToString();
            metric.OpcValue = val.Value;


            metric.Opcstatus = val.StatusCode.ToString();


            if (QualifyMetric(ref metric))
            {
                l_cache[metric.Measurement.Name] = metric.OpcValue;
                l_metricCollection.Add(metric);
            }
        }


        public void Dispose()
        {
            if (l_session != null)
            {
                l_session.RemoveSubscriptions(l_session.Subscriptions.ToList());
                l_session.Close();
                l_session.Dispose();
            }
            GC.SuppressFinalize(this);
        }




        #endregion
    }
}

using log4net;
using Odenwald;
using Odenwald.Common.Opc;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.OpcUaPlugin
{
    public class OpcUaPlugin : IMetricsReadPlugin
    {
        //TODO: 1. check last opc status 
        #region attributes
        static ILog l_logger = LogManager.GetLogger(typeof(OpcUaPlugin));
        string l_opcuaUrl;
        ApplicationConfiguration l_applicationConfig;
        OpcUaPluginConfig l_opcuaAdapterConfig;
        Session l_session;
        Subscription l_subscription;
        BlockingCollection<OpcMetric> l_metricCollection;
        List<PolledMeasurement> PolledMeasurements;
        List<MonitoredMeasurement> MonitoredMeasurements;
        static Dictionary<string, object> l_cache = new Dictionary<string, object>();
        #endregion

        #region imetricsreadplugin methods
        public void Configure()
        {
            l_opcuaAdapterConfig = ConfigurationManager.GetSection("OpcUa") as OpcUaPluginConfig;
            if (l_opcuaAdapterConfig == null)
            {
                throw new Exception("Cannot get configuration section : OpcUa");
            }
            l_opcuaUrl = l_opcuaAdapterConfig.Settings.Url;

            l_applicationConfig = new ApplicationConfiguration()
            {
                ApplicationName = "Odenwald",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = @"Windows",
                        StorePath = @"CurrentUser\My",
                        SubjectName = Utils.Format(@"CN={0}, DC={1}",
                       "Odenwald",
                       System.Net.Dns.GetHostName())
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = @"Windows",
                        StorePath = @"CurrentUser\TrustedPeople",
                    },
                    NonceLength = 32,
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
            };
            l_applicationConfig.Validate(ApplicationType.Client);
            if (l_applicationConfig.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                l_applicationConfig.CertificateValidator.CertificateValidation += (s, e) =>
                { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
            }


        }

        public void Flush()
        {
            l_logger.Debug("Flush");
        }

        public void Start()
        {
            InitializeMeasurements();
            l_session = Session.Create(l_applicationConfig, new ConfiguredEndpoint(null, new EndpointDescription(l_opcuaUrl)), true, "Odenwald", 60000, null, null);//EndpointDescription need to be changed according to your OPC server
            l_subscription = new Subscription(l_session.DefaultSubscription) { PublishingInterval = 1000 };

            l_metricCollection = new BlockingCollection<OpcMetric>();
            StartMonitoring();
            StartPolling();
        }

        public void Stop()
        {
            //unsubscribe
            l_subscription.RemoveItems(l_subscription.MonitoredItems);
            l_subscription.Delete(true);
            l_session.RemoveSubscription(l_subscription);
            //close
            l_session.Close();

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
        void InitializeMeasurements()
        {
            MonitoredMeasurements = new List<MonitoredMeasurement>();
            PolledMeasurements = new List<PolledMeasurement>();
            foreach (MeasurementConfig item in l_opcuaAdapterConfig.Measurements)
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
                
                MonitoredItem monitoredItem = new MonitoredItem()
                {
                    StartNodeId = nodeId,
                    AttributeId = Attributes.Value,
                    DisplayName = item.Path,
                    SamplingInterval = item.MonitorResolution
                };
                l_subscription.AddItem(monitoredItem);
                l_session.AddSubscription(l_subscription);

                l_subscription.Create();
                l_subscription.ApplyChanges();
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

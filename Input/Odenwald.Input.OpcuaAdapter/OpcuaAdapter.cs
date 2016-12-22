using log4net;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.Input.OpcuaAdapter
{
    public class MeasurementDto
    {
        public string Name { get; set; }

        public string DataType { get; set; }
        public string NodeId { get; set; }
        public string AttributeId { get; set; }
        public string Path { get; set; }
        public int DeadbandAbsolute { get; set; }
        public float DeadbandRelative { get; set; }
        public object LastValue { get; set; }
        public string LastOpcstatus { get; set; }
    }
    public class MonitoredMeasurement : MeasurementDto
    {
        public int MonitorResolution { get; set; }
    }
    public class PolledMeasurement : MeasurementDto
    {
        public int PollInterval { get; set; }
    }

    public class OpcuaAdapter : IInputAdapter, IDisposable
    {
        #region attributes
        static ILog l_logger = LogManager.GetLogger(typeof(OpcuaAdapter));
        string l_opcuaUrl;
        ApplicationConfiguration l_applicationConfig;
        OpcuaAdapterConfig l_opcuaAdapterConfig;
        Session l_session;
        Subscription l_subscription;
        BlockingCollection<OpcuaMetric> l_metricCollection;
        List<PolledMeasurement> PolledMeasurements;
        List<MonitoredMeasurement> MonitoredMeasurements;
        #endregion
        public IInputProcessor Processor { get; set; }


        public void Configure()
        {
            l_opcuaAdapterConfig = ConfigurationManager.GetSection("Opcua") as OpcuaAdapterConfig;
            if (l_opcuaAdapterConfig == null)
            {
                throw new Exception("Cannot get configuration section : Opcua");
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
            throw new NotImplementedException();
        }

        public void Start()
        {
            InitializeMeasurements();
            l_session = Session.Create(l_applicationConfig, new ConfiguredEndpoint(null, new EndpointDescription(l_opcuaUrl)), true, "Odenwald", 60000, null, null);//EndpointDescription need to be changed according to your OPC server
            l_subscription = new Subscription(l_session.DefaultSubscription) { PublishingInterval = 1000 };

            l_metricCollection = new BlockingCollection<OpcuaMetric>();
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


        public IList<IInputMetric> Read()
        {
            if (l_metricCollection.Count <= 0)
                return null;
            List<OpcuaMetric> metricList = new List<OpcuaMetric>();


            while (l_metricCollection.Count > 0)
            {

                OpcuaMetric data = null;
                try
                {
                    data = l_metricCollection.Take();
                }
                catch (InvalidOperationException) { }

                if (data != null)
                {
                    metricList.Add(data);
                }
            }
            l_logger.DebugFormat("Read {0} items.", metricList.Count);
            return new List<IInputMetric>(metricList.Cast<IInputMetric>());

        }

        #region methods
        void InitializeMeasurements()
        {
            MonitoredMeasurements = new List<MonitoredMeasurement>();
            PolledMeasurements = new List<PolledMeasurement>();
            foreach (MeasurementConfig item in l_opcuaAdapterConfig.Measurements)
            {

                switch (item.CollectionType)
                {
                    case "monitored":
                        var monitoredpoint = new MonitoredMeasurement()
                        {
                            Name = item.Name,
                            NodeId = item.NodeId,
                            Path = item.Path,
                            MonitorResolution = item.MonitorResolution > 0 ? item.MonitorResolution : 0,
                            DeadbandAbsolute = item.DeadbandAbsolute > 0 ? item.DeadbandAbsolute : 0,
                            DeadbandRelative = item.DeadbandRelative > 0 ? item.DeadbandRelative : 0,
                            DataType = item.DataType
                        };

                        MonitoredMeasurements.Add(monitoredpoint);
                        break;
                    case "polled":
                        var polledPoint = new PolledMeasurement()
                        {
                            Name = item.Name,
                            NodeId = item.NodeId,
                            Path = item.Path,
                            DeadbandAbsolute = item.DeadbandAbsolute > 0 ? item.DeadbandAbsolute : 0,
                            DeadbandRelative = item.DeadbandRelative > 0 ? item.DeadbandRelative : 0,
                            PollInterval = item.PollInterval,
                            DataType = item.DataType
                        };

                        PolledMeasurements.Add(polledPoint);

                        break;
                    default:
                        //no collection type, log
                        break;
                }
            }
        }
        void StartMonitoring()
        {
            foreach (var item in MonitoredMeasurements)
            {
                NodeId nodeItem = new NodeId(item.NodeId);
                MonitoredItem monitoredItem = new MonitoredItem()
                {
                    StartNodeId = item.NodeId,
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
            OpcuaMetric metric = new OpcuaMetric();
            metric.HostName = "localhost";
            metric.AdapterInstanceName = "OpcuaAdapterInstance";
            metric.AdapterName = "OpcuaAdapter";

            metric.Timestamp = NewValue.SourceTimestamp;
            metric.OpcValue = NewValue.WrappedValue.Value;
            metric.TypeName = "gauge";
            metric.TypeInstanceName = "gauge";
            metric.Interval = measurement.MonitorResolution;

            metric.Measurement = (MeasurementDto)measurement;
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            double epoch = t.TotalMilliseconds / 1000;
            metric.Epoch = Math.Round(epoch, 3);

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
            OpcuaMetric metric = new OpcuaMetric();
            metric.HostName = "localhost";
            metric.AdapterInstanceName = "OpcuaAdapterInstance";
            metric.AdapterName = "OpcuaAdapter";


            metric.TypeName = "gauge";
            metric.TypeInstanceName = "gauge";
            metric.Interval = measurement.PollInterval;

            metric.Measurement = (MeasurementDto)measurement;
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            double epoch = t.TotalMilliseconds / 1000;
            metric.Epoch = Math.Round(epoch, 3);



            var readValue = new ReadValueId
            {
                NodeId = measurement.NodeId,
                AttributeId = Attributes.Value
            };
            var nodesToRead = new ReadValueIdCollection() { readValue };
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
            metric.Measurement.LastOpcstatus = val.StatusCode.ToString();
            metric.OpcValue = val.Value;
            metric.Timestamp = val.SourceTimestamp;
            l_metricCollection.Add(metric);
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

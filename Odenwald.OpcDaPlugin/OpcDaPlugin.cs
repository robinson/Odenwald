using log4net;
using Odenwald.Common.Opc;
using Opc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Factory = OpcCom.Factory;
using OpcDa = Opc.Da;

namespace Odenwald.OpcDaPlugin
{
    public class OpcDaPlugin : IMetricsReadPlugin, IDisposable
    {
        #region attributes
        static ILog l_logger = LogManager.GetLogger(typeof(OpcDaPlugin));
        List<PolledMeasurement> PolledMeasurements;
        List<MonitoredMeasurement> MonitoredMeasurements;
        static Dictionary<string, object> l_cache = new Dictionary<string, object>();
        BlockingCollection<OpcMetric> l_metricCollection;
        OpcPluginConfig l_opcDaPluginConfig;
        OpcDa.Server l_server;
        
        #endregion

        #region IMetricsReadPlugin
        public void Configure()
        {
            l_opcDaPluginConfig = OpcPluginConfig.GetConfig("OpcDa");
            if (l_opcDaPluginConfig == null)
            {
                throw new Exception("Cannot get configuration section : OpcUa");
            }

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
                        PluginInstanceName = "OpcDaPluginInstance",
                        PluginName = "OpcDaPlugin",
                        Extension = data.JsonString()
                    };
                    metricList.Add(metricValue);
                }
            }
            l_logger.DebugFormat("Read {0} items.", metricList.Count);
            return metricList;
        }

        public void Start()
        {
            InitializeMeasurements();
            Uri serverUri = new Uri(l_opcDaPluginConfig.Settings.Url);
            var opcServerUrl = new URL(serverUri.ToString())
            {
                Scheme = serverUri.Scheme,
                HostName = serverUri.Host
            };
            l_server = new OpcDa.Server(new Factory(), opcServerUrl);
            l_server.Connect();

            l_metricCollection = new BlockingCollection<OpcMetric>();
            StartMonitoring();
            StartPolling();
        }

        public void Stop()
        {
            if (l_server != null)
                l_server.Dispose();
        }
        #endregion

        #region methods
        void InitializeMeasurements()
        {
            MonitoredMeasurements = new List<MonitoredMeasurement>();
            PolledMeasurements = new List<PolledMeasurement>();
            foreach (MeasurementConfig item in l_opcDaPluginConfig.Measurements)
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
                var subItem = new OpcDa.SubscriptionState
                {
                    Name = item.Name,
                    Active = true,
                    UpdateRate = item.MonitorResolution
                };
                var sub = l_server.CreateSubscription(subItem);
                sub.DataChanged += (handle, requestHandle, values) =>
                {
                    var p = values[0];
                    MonitorData(item, p);
                };
                sub.AddItems(new[] { new OpcDa.Item { ItemName = item.Path } });
                sub.SetEnabled(true);
            }
        }

        private void MonitorData(MonitoredMeasurement measurement, OpcDa.ItemValueResult p)
        {
            OpcMetric metric = new OpcMetric()
            {
                Timestamp = p.Timestamp,
                OpcValue = p.Value,
                Opcstatus = p.ResultID.ToString(),
                Measurement = (MeasurementDto)measurement
            };
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
            var item = new OpcDa.Item { ItemName = measurement.Path };
            if (l_server == null || l_server.GetStatus().ServerState != OpcDa.serverState.running)
            {
                l_logger.ErrorFormat(string.Format("Server not connected. Cannot read path {0}.", measurement.Name));
                throw new Exception(string.Format("Server not connected. Cannot read path {0}.",measurement.Name));
            }
            var result = l_server.Read(new[] { item })[0];
            if (result == null)
            {
                l_logger.Error("the server replied with an empty response!!!");
                throw new Exception("the server replied with an empty response");//if any item cannot read, throw exeption
            }
            if (result.ResultID.ToString() != "S_OK")
            {
                l_logger.ErrorFormat(string.Format("Invalid response from the server. (Response Status: {0}, Opc Tag: {1})", result.ResultID, measurement.Path));
                throw new Exception(string.Format("Invalid response from the server. (Response Status: {0}, Opc Tag: {1})", result.ResultID, measurement.Path));
            }

            OpcMetric metric = new OpcMetric()
            {
                Measurement = measurement,
                Opcstatus = result.ResultID.ToString(),
                OpcValue = result.Value,
                Timestamp = result.Timestamp

            };
            if (OpcHelper.QualifyMetric(ref metric, l_cache))
            {
                l_cache[metric.Measurement.Name] = metric.OpcValue;
                l_metricCollection.Add(metric);
            }
        }
       
        public void Dispose()
        {
            if (l_server != null)
                l_server.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

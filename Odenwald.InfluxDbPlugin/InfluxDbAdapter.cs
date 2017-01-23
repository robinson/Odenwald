using InfluxDB.Net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using InfluxDB.Net.Models;
using log4net;

namespace Odenwald.InfluxDbPlugin
{
    public class InfluxDbAdapter : IMetricsWritePlugin
    {
        #region attributes
        static ILog l_logger = LogManager.GetLogger(typeof(InfluxDbAdapter));
        InfluxDbAdapterConfig l_influxDbConfig;
        InfluxDb l_influxDbClient;
        #endregion

        #region IOutputAdapter properties

        public void Configure()
        {
            l_influxDbConfig = ConfigurationManager.GetSection("InfluxDb") as InfluxDbAdapterConfig;
            if (l_influxDbConfig == null)
            {
                throw new Exception("Cannot get configuration section : InfluxDb");
            }
        }

        public void Flush()
        {
            l_logger.Info("InfluxDbAdapter flushed!");
        }

        public void Start()
        {
            l_influxDbClient = new InfluxDb(l_influxDbConfig.Settings.Url, l_influxDbConfig.Settings.Username, l_influxDbConfig.Settings.Password);

        }

        public void Stop()
        {
            l_logger.Info("InfluxDbAdapter stopped!");
        }
        

        public void Write(MetricValue metric)
        {
            var p = new Point()
            {
                Measurement = metric.HostName,
                Precision = InfluxDB.Net.Enums.TimeUnit.Seconds,
                Fields = new Dictionary<string, object>()
                {
                    {"Value", metric.Values},
                    {"Timestamp", DateTime.Now}
                },
                Tags = metric.MetaData.ToDictionary(k => k.Key, k => k.Value == "" || k.Value == null ? null : (object)k.Value)
            };
            var writeResponse = l_influxDbClient.WriteAsync(l_influxDbConfig.Settings.Database, p);

            if (writeResponse.IsFaulted || writeResponse.IsCanceled)
                l_logger.ErrorFormat("InfluxDb write false: {0}", writeResponse.Exception);
        }
        #endregion
    }
}

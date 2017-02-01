using InfluxDB.Net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using InfluxDB.Net.Models;
using log4net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Odenwald.InfluxDbPlugin
{
    public class InfluxDbPlugin : IMetricsWritePlugin
    {
        #region attributes
        static ILog l_logger = LogManager.GetLogger(typeof(InfluxDbPlugin));
        InfluxDbPluginConfig l_influxDbConfig;
        InfluxDb l_influxDbClient;
        #endregion

        #region IOutputAdapter properties

        public void Configure()
        {
            l_influxDbConfig = ConfigurationManager.GetSection("InfluxDb") as InfluxDbPluginConfig;
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
            if (metric.Extension.Equals("")) // no extension, nothing to write
                return;
            var extension = JObject.Parse(metric.Extension);
            if (extension["OpcValue"] == null) // no OpcValue, nothing to write
                return;
            var opcValue = extension["OpcValue"];
            var opcName = extension["Name"].ToString();
            var timeStamp = extension["Timestamp"] != null? (DateTime)extension["Timestamp"]:DateTime.Now;
            Dictionary<string, object> tags = null;
            if (extension["Tags"] != null)
                tags = JsonConvert.DeserializeObject<Dictionary<string, object>>((string)extension["Tags"]);
            var p = new Point()
            {
                Measurement = opcName,
                Precision = InfluxDB.Net.Enums.TimeUnit.Seconds,
                Fields = new Dictionary<string, object>()
                {
                    {"Value",opcValue},
                    {"Timestamp", timeStamp}
                },
                Tags = tags// metric.MetaData.ToDictionary(k => k.Key, k => k.Value == "" || k.Value == null ? null : (object)k.Value)
            };
            var writeResponse = l_influxDbClient.WriteAsync(l_influxDbConfig.Settings.Database, p);

            if (writeResponse.IsFaulted || writeResponse.IsCanceled)
                l_logger.ErrorFormat("InfluxDb write false: {0}", writeResponse.Exception);
        }
        #endregion
    }
}

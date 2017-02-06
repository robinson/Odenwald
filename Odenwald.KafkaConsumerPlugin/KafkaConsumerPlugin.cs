using KafkaNet;
using KafkaNet.Common;
using KafkaNet.Model;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Odenwald.KafkaConsumerPlugin
{
    public class KafkaConsumerPlugin : IMetricsReadPlugin, IDisposable
    {
        #region attributes
        static ILog l_logger = LogManager.GetLogger(typeof(KafkaConsumerPlugin));
        KafkaConsumerPluginConfig l_kafkaConsumerConfig;
        Consumer l_kafkaConsumer;
        BlockingCollection<string> l_metricCollection;
        #endregion
        public void Configure()
        {
            l_kafkaConsumerConfig = KafkaConsumerPluginConfig.GetConfig();
            if (l_kafkaConsumerConfig == null)
            {
                throw new Exception("Cannot get configuration section : KafkaConsumer");
            }
        }

        public IList<MetricValue> Read()
        {
            if (l_metricCollection.Count <= 0)
                return null;
            List<MetricValue> metricList = new List<MetricValue>();
            while (l_metricCollection.Count > 0)
            {

                string data = null;
                try
                {
                    data = l_metricCollection.Take();
                }
                catch (InvalidOperationException) { }

                if (data != null)
                {
                    var metricValue = JsonConvert.DeserializeObject<MetricValue>(data);//this is not a good practice, review later

                    metricList.Add(metricValue);
                }
            }
            l_logger.DebugFormat("Read {0} items.", metricList.Count);
            return metricList;
        }

        public void Start()
        {
            var urls = l_kafkaConsumerConfig.Settings.Url.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
               .Select(x => new Uri(x));

            var options = new KafkaOptions(urls.ToArray())
            {
                Log = new DefaultTraceLog()
            };
            l_metricCollection = new BlockingCollection<string>();
            Task.Run(() =>
            {
                l_kafkaConsumer = new Consumer(new ConsumerOptions(l_kafkaConsumerConfig.Settings.Topic, new BrokerRouter(options)) { Log = new DefaultTraceLog() });
                foreach (var message in l_kafkaConsumer.Consume())
                {
                    var value = message.Value.ToUtf8String();

                    l_metricCollection.Add(value);
                }
            });
        }

        public void Stop()
        {

        }
        public void Dispose()
        {
            if (l_kafkaConsumer != null)
                l_kafkaConsumer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using log4net;
using System;
using System.Configuration;
using System.Linq;

namespace Odenwald.KafkaProducerPlugin
{
    public class KafkaProducerPlugin : IMetricsWritePlugin, IDisposable
    {
        #region attributes
        static ILog l_logger = LogManager.GetLogger(typeof(KafkaProducerPlugin));
        KafkaProducerPluginConfig l_kafkaProducerConfig;
        Producer l_kafkaProducer;
        #endregion
        public void Configure()
        {
            l_kafkaProducerConfig = KafkaProducerPluginConfig.GetConfig();
            if (l_kafkaProducerConfig == null)
            {
                throw new Exception("Cannot get configuration section : KafkaProducer");
            }

        }

        public void Flush()
        {
            l_logger.Info("kafka producer flushed!");
        }

        public void Start()
        {
            var urls = l_kafkaProducerConfig.Settings.Url.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => new Uri(x));

            var options = new KafkaOptions(urls.ToArray())
            {
                Log = new DefaultTraceLog()
            };
            var batchDelayTime = l_kafkaProducerConfig.Settings.BatchDelayTime == 0 ? 2000 : l_kafkaProducerConfig.Settings.BatchDelayTime;
            var batchSize = l_kafkaProducerConfig.Settings.BatchSize == 0 ? 100 : l_kafkaProducerConfig.Settings.BatchSize;
            l_kafkaProducer = new Producer(new BrokerRouter(options))
            {
                BatchDelayTime = TimeSpan.FromMilliseconds(batchDelayTime),
                BatchSize = batchSize
            };
        }

        public void Stop()
        {
            l_kafkaProducer.Stop();
        }

        public void Write(MetricValue metric)
        {
            var produceResponse = l_kafkaProducer.SendMessageAsync(l_kafkaProducerConfig.Settings.Topic, new[] { new Message(metric.GetJsonString()) });
            if (produceResponse.IsFaulted || produceResponse.IsCanceled)
            {
                l_logger.ErrorFormat("kafka producer write false: {0}", produceResponse.Exception);
            }
        }

        public void Dispose()
        {
            if (l_kafkaProducer != null)
                l_kafkaProducer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

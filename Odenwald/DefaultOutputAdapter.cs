using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald
{
    internal class DefaultOutputAdapter : IOutputAdapter
    {
        static ILog l_logger = LogManager.GetLogger(typeof(DefaultOutputAdapter));
        public IOutputProcessor Processor
        {
            get;set;
        }

        public void Configure()
        {
            l_logger.Info("default output adapter configured");
        }

        public void Flush()
        {
            Console.WriteLine("default output adapter: flushing");
        }

        public void Start()
        {
            l_logger.Info("default output adapter started");
        }

        public void Stop()
        {
            l_logger.Info("default output adapter stopped");
        }

        public void Write(IOutputMetric metric)
        {
            l_logger.InfoFormat("default output adapter: {0}", metric.GetJsonString());
            Console.WriteLine("default output adapter: {0}", metric.GetJsonString());
        }
    }
}

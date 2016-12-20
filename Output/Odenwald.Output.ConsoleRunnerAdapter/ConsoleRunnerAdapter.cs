using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.Output.ConsoleRunnerAdapter
{
    public class ConsoleRunnerAdapter : IOutputAdapter
    {
        static ILog l_logger = LogManager.GetLogger(typeof(ConsoleRunnerAdapter));
        public IOutputProcessor Processor
        {
            get;set;
        }

        public void Configure()
        {
            l_logger.Info("console runner output adapter configured");
        }

        public void Flush()
        {
            Console.WriteLine("console runner output adapter: flushing");
        }

        public void Start()
        {
            l_logger.Info("console runner output adapter started");
        }

        public void Stop()
        {
            l_logger.Info("console runner output adapter stopped");
        }

        public void Write(IOutputMetric metric)
        {
            l_logger.InfoFormat("console runner output adapter: {0}", metric.GetJsonString());
            Console.WriteLine("console runner output adapter: {0}", metric.GetJsonString());
        }
    }
}

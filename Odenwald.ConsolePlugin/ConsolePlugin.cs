using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.ConsolePlugin
{
    public class ConsolePlugin : IMetricsWritePlugin
    {
        static ILog l_logger = LogManager.GetLogger(typeof(ConsolePlugin));
        

        public void Configure()
        {
            l_logger.Info("console runner output configured");
        }

        public void Flush()
        {
            Console.WriteLine("console runner output  flushing");
        }

        public void Start()
        {
            l_logger.Info("console runner output started");
        }

        public void Stop()
        {
            l_logger.Info("console runner output stopped");
        }

        public void Write(MetricValue metric)
        {
            l_logger.InfoFormat("console runner output {0}", metric.GetJsonString());
            Console.WriteLine("console runner output {0}", metric.GetJsonString());
        }       
    }
}

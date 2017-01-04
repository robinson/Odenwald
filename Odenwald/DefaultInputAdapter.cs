using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald
{
    internal class DefaultInputAdapter : IInputAdapter
    {
        static ILog l_logger = LogManager.GetLogger(typeof(DefaultInputAdapter));
        public IInputProcessor Processor
        {
            get;set;
        }

        public void Configure()
        {
            l_logger.Info("default input adapter configured");
        }

        /// <summary>
        /// generate double
        /// </summary>
        /// <returns></returns>
        public IList<IInputMetric> Read()
        {
            var metric = (IInputMetric)Activator.CreateInstance(Processor.InputMetric.GetType());
            metric.HostName = "localhost";
            metric.AdapterInstanceName = "DefaultAdapterInstance";
            metric.AdapterName = "DefaultAdapterName";
            
            metric.Values = new double[] { DateTime.Now.ToOADate() };
            metric.TypeName = "gauge";
            metric.TypeInstanceName = "gauge";
            metric.Epoch = Util.GetNow();


            return new List<IInputMetric> { metric };

        }

        public void Start()
        {
            l_logger.Info("default input adapter started");
        }

        public void Stop()
        {
            l_logger.Info("default input adapter stopped");
        }
    }
}

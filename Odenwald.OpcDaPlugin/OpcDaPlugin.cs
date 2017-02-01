using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.OpcDaPlugin
{
    public class OpcDaPlugin : IMetricsReadPlugin
    {
        #region attributes
        static ILog l_logger = LogManager.GetLogger(typeof(OpcDaPlugin));
        #endregion
        public void Configure()
        {
            throw new NotImplementedException();
        }

        public IList<MetricValue> Read()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}

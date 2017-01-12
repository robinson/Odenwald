using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald
{
    public interface IMetricsReadPlugin : IMetricsPlugin
    {
        IList<MetricValue> Read();
    }
}

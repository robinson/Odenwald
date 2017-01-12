using System.Collections.Generic;
using System.Threading.Tasks;

namespace Odenwald
{
    public interface IMetricsWritePlugin : IMetricsPlugin
    {
        void Write(MetricValue metric);
        void Flush();

    }
}

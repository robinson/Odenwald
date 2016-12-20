using System.Collections.Generic;
using System.Threading.Tasks;

namespace Odenwald
{
    public interface IOutputAdapter: IAdapter
    {
        void Write(IOutputMetric metric);
        void Flush();
        
        IOutputProcessor Processor { get; set; }

    }
}

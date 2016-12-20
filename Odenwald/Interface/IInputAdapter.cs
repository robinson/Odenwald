using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald
{
    public interface IInputAdapter: IAdapter
    {
        IList<IInputMetric> Read();
        IInputProcessor Processor { get; set; }
    }
}

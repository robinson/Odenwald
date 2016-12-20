using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald
{

    public interface IInputProcessor
    {
        void ProcessInput(ref IInputMetric metric);
        IInputMetric InputMetric { get; set; }
    }
}

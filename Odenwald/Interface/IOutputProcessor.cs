using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald
{
    public interface IOutputProcessor 
    {
        void ProcessInput(IInputMetric inputMetric);
        IOutputMetric OutputMetric { get; set; }
        IInputMetric RawMetric { get; set; }

    }
}

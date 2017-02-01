using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.Common.Opc
{
    public class MeasurementDto
    {
        public string Name { get; set; }

        public string DataType { get; set; }
        public string NodeId { get; set; }
        public string AttributeId { get; set; }
        public string Path { get; set; }
        public int DeadbandAbsolute { get; set; }
        public decimal DeadbandRelative { get; set; }
        public object LastValue { get; set; }
        public string LastOpcstatus { get; set; }
    }
    public class MonitoredMeasurement : MeasurementDto
    {
        public int MonitorResolution { get; set; }
    }
    public class PolledMeasurement : MeasurementDto
    {
        public int PollInterval { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.Output.InfluxDbAdapter
{
    public class InfluxDbMetric : IOutputMetric
    {
        public string AdapterInstanceName { get; set; }

        public string AdapterName { get; set; }

        public IList<string> DsNames { get; set; }

        public IList<string> DsTypes { get; set; }

        public double Epoch { get; set; }

        public string HostName { get; set; }

        public int Interval { get; set; }
        public string Key { get; }

        public IDictionary<string, string> MetaData { get;}

        public string TypeInstanceName { get; set; }

        public string TypeName { get; set; }
        public double[] Values { get; set; }
        public void AddMetaData(IDictionary<string, string> meta)
        {
         
        }

        public void AddMetaData(string tagName, string tagValue)
        {
         
        }

        public IMetric DeepCopy()
        {
            throw new NotImplementedException();
        }

        public string GetJsonString()
        {
            throw new NotImplementedException();
        }
    }
}

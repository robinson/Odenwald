using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald
{
    public interface IMetric
    {
        string HostName { get; set; }
        string AdapterName { get; set; }
        string AdapterInstanceName { get; set; }
        string TypeName { get; set; }
        string TypeInstanceName { get; set; }
        double Epoch { get; set; }
        int Interval { get; set; }
        double[] Values { get; set; }
        IList<string> DsTypes { get; set; }
        IList<string> DsNames { get; set; }
        IDictionary<string, string> MetaData { get;}
        void AddMetaData(string tagName, string tagValue);
        void AddMetaData(IDictionary<string, string> meta);
        string GetJsonString();
        string Key { get; }
        IMetric DeepCopy();

    }
}

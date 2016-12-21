using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.Input.OpcuaAdapter
{
    public class OpcuaMetric : IInputMetric, IOutputMetric
    {
        #region Attribute
        static ILog l_logger = LogManager.GetLogger(typeof(OpcuaMetric));
        private readonly IDictionary<string, string> l_meta = new SortedDictionary<string, string>();
        #endregion

        #region IInputMetric properties

        public string AdapterInstanceName { get; set; }

        public string AdapterName { get; set; }

        public IList<string> DsNames { get; set; }

        public IList<string> DsTypes { get; set; }
        public double Epoch { get; set; }

        public string HostName { get; set; }

        public int Interval { get; set; }

        public string Key { get; }


        public IDictionary<string, string> MetaData { get; set; }

        public string TypeInstanceName { get; set; }
        public string TypeName { get; set; }
        public double[] Values { get; set; }
        public object OpcValue { get; set; }

        public void AddMetaData(IDictionary<string, string> meta)
        {
            if (meta == null)
            {
                return;
            }

            foreach (var tag in meta)
            {
                l_meta[tag.Key] = tag.Value;
            }
        }

        public void AddMetaData(string tagName, string tagValue)
        {
            throw new NotImplementedException();
        }

        public IMetric DeepCopy()
        {
            var other = (OpcuaMetric)MemberwiseClone();
            other.HostName = String.Copy(HostName);
            other.AdapterName = String.Copy(AdapterName);
            other.AdapterInstanceName = String.Copy(AdapterInstanceName);
            other.TypeName = String.Copy(TypeName);
            other.TypeInstanceName = String.Copy(TypeInstanceName);
            other.OpcValue = OpcValue;
            return (other);
        }

        public string GetJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }
        #endregion

        public MeasurementDto Measurement { get; set; }


    }
}

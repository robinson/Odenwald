using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald
{
    public class MetricValue
    {
        #region Attribute
        static ILog l_logger = LogManager.GetLogger(typeof(MetricValue));
        private readonly IDictionary<string, string> l_meta = new SortedDictionary<string, string>();
        #endregion

        #region IInputMetric Properties
        public string PluginName { get; set; }
        public string PluginInstanceName { get; set; }

        public string HostName { get; set; }

        public IDictionary<string, string> MetaData { get { return l_meta; } }

        public string TypeName { get; set; }
        public string TypeInstanceName { get; set; }

        public double[] Values { get; set; }

        public double Epoch { get { return DateTimeOffset.Now.ToUnixTimeSeconds(); } }

        public int Interval { get; set; }

        public IList<string> DsTypes { get; set; }

        public IList<string> DsNames { get; set; }

        public string Key
        {
            get
            {
                return (HostName + "." + PluginName + "." + PluginName + "." + TypeName + "." + TypeInstanceName);
            }
        }

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
            l_meta[tagName] = tagValue;
        }
        public string Extension { get; set; } //extension will be a json string
        #endregion

        #region Properties
        public MetricValue DeepCopy()
        {
            var other = (MetricValue)MemberwiseClone();
            other.HostName = String.Copy(HostName);
            other.PluginName = String.Copy(PluginName);
            other.PluginInstanceName = String.Copy(PluginInstanceName);
            other.TypeName = String.Copy(TypeName);
            other.TypeInstanceName = String.Copy(TypeInstanceName);
            other.Values = (double[])Values.Clone();
            return (other);
        }

        public string GetJsonString()
        {
            IList<DataSource> dsList = DataSetCollection.Instance.GetDataSource(TypeName);
            DsNames = new List<string>();
            DsTypes = new List<string>();
            if (dsList == null)
            {
                l_logger.DebugFormat("Invalid type : {0}, not found in types.db", TypeName);
            }
            else
            {
                foreach (DataSource ds in dsList)
                {
                    DsNames.Add(ds.Name);
                    DsTypes.Add(ds.Type.ToString().ToLower());
                }
            }
            var jsonString = "";
            try
            {
                jsonString = JsonConvert.SerializeObject(this);
            }
            catch (Exception ex)
            {
                l_logger.ErrorFormat("Got exception in json conversion : {0}", ex);
            }
            return (jsonString);
        }       
        #endregion


    }
}

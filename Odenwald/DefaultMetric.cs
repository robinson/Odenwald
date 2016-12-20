using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald
{
    public sealed class DefaultMetric : IInputMetric, IOutputMetric
    {
        #region Attribute
        static ILog l_logger = LogManager.GetLogger(typeof(DefaultMetric));
        private readonly IDictionary<string, string> l_meta = new SortedDictionary<string, string>();
        #endregion

        #region IInputMetric Properties
        public string AdapterInstanceName { get; set; }

        public string AdapterName { get; set; }

        public string HostName { get; set; }

        public IDictionary<string, string> MetaData { get { return l_meta; } }

        public string TypeName { get; set; }
        public string TypeInstanceName { get; set; }

        public double[] Values { get; set; }

        public double Epoch { get; set; }

        public int Interval { get; set; }

        public IList<string> DsTypes { get; set; }

        public IList<string> DsNames { get; set; }

        public string Key
        {
            get
            {
                return (HostName + "." + AdapterName + "." + AdapterName + "." + TypeName + "." + TypeInstanceName);
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
        #endregion

        #region Properties
        //public DefaultMetric DeepCopy()
        //{
        //    var other = (DefaultMetric)MemberwiseClone();
        //    other.HostName = String.Copy(HostName);
        //    other.AdapterName = String.Copy(AdapterName);
        //    other.AdapterInstanceName = String.Copy(AdapterInstanceName);
        //    other.TypeName = String.Copy(TypeName);
        //    other.TypeInstanceName = String.Copy(TypeInstanceName);
        //    other.Values = (double[])Values.Clone();
        //    return (other);
        //}

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

        IMetric IMetric.DeepCopy()
        {
            var other = (DefaultMetric)MemberwiseClone();
            other.HostName = String.Copy(HostName);
            other.AdapterName = String.Copy(AdapterName);
            other.AdapterInstanceName = String.Copy(AdapterInstanceName);
            other.TypeName = String.Copy(TypeName);
            other.TypeInstanceName = String.Copy(TypeInstanceName);
            other.Values = (double[])Values.Clone();
            return (other);
        }
        #endregion


    }
}

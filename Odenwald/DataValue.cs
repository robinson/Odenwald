using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Odenwald
{
    public class DataValue
    {
        const string l_jsonFormat =
           @"{{""host"":""{0}"", ""plugin"":""{1}"", ""plugin_instance"":""{2}""," +
           @" ""type"":""{3}"", ""type_instance"":""{4}"", ""time"":{5}, ""interval"":{6}," +
           @" ""dstypes"":[{7}], ""dsnames"":[{8}], ""values"":[{9}]{10}}}";
        const string l_metaDataJsonFormat = @", ""meta"":{0}";

        static ILog l_logger = LogManager.GetLogger(typeof(DataValue));

        readonly IDictionary<string, string> l_meta = new SortedDictionary<string, string>();

        public string HostName { get; set; }
        public string PluginName { get; set; }
        public string PluginInstanceName { get; set; }
        public string TypeName { get; set; }
        public string TypeInstanceName { get; set; }

        public int Interval { get; set; }
        public double Epoch { get; set; }
        public object[] Values { get; set; }

        public IDictionary<string, string> MetaData
        {
            get
            {
                return l_meta;
            }
        }

        public void AddMetaData(string tagName, string tagValue)
        {
            l_meta[tagName] = tagValue;
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

        public string Key()
        {
            return (HostName + "." + PluginName + "." + PluginInstanceName + "." + TypeName + "." + TypeInstanceName);
        }

        public DataValue Clone()
        {
            var other = (DataValue)MemberwiseClone();
            other.HostName = String.Copy(HostName);
            other.PluginName = String.Copy(PluginName);
            other.PluginInstanceName = String.Copy(PluginInstanceName);
            other.TypeName = String.Copy(TypeName);
            other.TypeInstanceName = String.Copy(TypeInstanceName);
            other.Values = (object[])Values.Clone();
            return (other);
        }

        public string EscapeString(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return (str);
            }
            return (str.Replace(@"\", @"\\"));
        }

        public string GetMetaDataJsonStr()
        {
            return JsonConvert.SerializeObject(MetaData);
        }

        public string GetDataJsonStr()
        {
            IList<DataSource> dsList = DataSetCollection.Instance.GetDataSource(TypeName);
            var dsNames = new List<string>();
            var dsTypes = new List<string>();
            if (dsList == null)
            {
                l_logger.Debug(string.Format( "Invalid type : {0}, not found in types.db", TypeName));
            }
            else
            {
                foreach (DataSource ds in dsList)
                {
                    dsNames.Add(ds.Name);
                    dsTypes.Add(ds.Type.ToString().ToLower());
                }
            }
            String epochStr = Epoch.ToString();
            string dsTypesStr = string.Join(",", dsTypes.ConvertAll(m => string.Format("\"{0}\"", m)).ToArray());
            string dsNamesStr = string.Join(",", dsNames.ConvertAll(m => string.Format("\"{0}\"", m)).ToArray());
            string valStr = string.Join(",", Array.ConvertAll(Values, val => val.ToString()));


            var metaDataStr = "";
            if (MetaData.Count > 0)
            {
                metaDataStr = string.Format(l_metaDataJsonFormat, GetMetaDataJsonStr());
            }
            var res = "";
            try
            {
                res = string.Format(l_jsonFormat, HostName, PluginName,
                    EscapeString(PluginInstanceName), TypeName, EscapeString(TypeInstanceName), epochStr,
                    Interval, dsTypesStr, dsNamesStr, valStr, metaDataStr);
            }
            catch (Exception exp)
            {
                l_logger.Error("Got exception in json conversion : {0}", exp);
            }
            return (res);
        }
    }
}

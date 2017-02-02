using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.Common.Opc
{
    public sealed class OpcPluginConfig : ConfigurationSection
    {
        public static OpcPluginConfig GetConfig(string rootElement)
        {
            return (OpcPluginConfig)ConfigurationManager.GetSection(rootElement) ?? new OpcPluginConfig();
        }
        [ConfigurationProperty("Settings", IsRequired = true)]
        public SettingsConfig Settings
        {
            get { return (SettingsConfig)base["Settings"]; }
            set { base["Settings"] = value; }
        }
        [ConfigurationProperty("Measurements", IsRequired = true)]
        [ConfigurationCollection(typeof(MeasurementCollection), AddItemName = "Measurement")]
        public MeasurementCollection Measurements
        {
            get { return (MeasurementCollection)base["Measurements"]; }
            set { base["Measurements"] = value; }
        }
    }
    public sealed class SettingsConfig : ConfigurationElement
    {
        [ConfigurationProperty("FailoverTimeout", IsRequired = true)]
        public int FailoverTimeout
        {
            get { return (int)base["FailoverTimeout"]; }
            set { base["FailoverTimeout"] = value; }
        }
        [ConfigurationProperty("Url", IsRequired = true)]
        public string Url
        {
            get { return (string)base["Url"]; }
            set { base["Url"] = value; }
        }
    }
    public sealed class MeasurementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new MeasurementConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (((MeasurementConfig)element).UniqueId);
        }
    }
    public sealed class MeasurementConfig : ConfigurationElement
    {
        internal Guid UniqueId { get; set; }
        public MeasurementConfig()
        {
            UniqueId = Guid.NewGuid();
        }
        [ConfigurationProperty("Name", IsRequired = true)]
        public string Name
        {
            get { return (string)base["Name"]; }
            set { base["Name"] = value; }
        }
        [ConfigurationProperty("DataType", IsRequired = true)]
        public string DataType
        {
            get { return (string)base["DataType"]; }
            set { base["DataType"] = value; }
        }

        [ConfigurationProperty("Path", IsRequired = true)]
        public string Path
        {
            get { return (string)base["Path"]; }
            set { base["Path"] = value; }
        }
        //[ConfigurationProperty("NodeId", IsRequired = true)]
        //public string NodeId
        //{
        //    get { return (string)base["NodeId"]; }
        //    set { base["NodeId"] = value; }
        //}
        [ConfigurationProperty("CollectionType", IsRequired = true)]
        public string CollectionType
        {
            get { return (string)base["CollectionType"]; }
            set { base["CollectionType"] = value; }
        }
        [ConfigurationProperty("PollInterval")]
        public int PollInterval
        {
            get { return (int)base["PollInterval"]; }
            set { base["PollInterval"] = value; }
        }
        [ConfigurationProperty("MonitorResolution")]
        public int MonitorResolution
        {
            get { return (int)base["MonitorResolution"]; }
            set { base["MonitorResolution"] = value; }
        }
        [ConfigurationProperty("DeadbandAbsolute", IsRequired = true)]
        public int DeadbandAbsolute
        {
            get { return (int)base["DeadbandAbsolute"]; }
            set { base["DeadbandAbsolute"] = value; }
        }
        [ConfigurationProperty("DeadbandRelative", IsRequired = true)]
        public decimal DeadbandRelative
        {
            get { return (decimal)base["DeadbandRelative"]; }
            set { base["DeadbandRelative"] = value; }
        }
        [ConfigurationProperty("Tags")]
        public string Tags
        {
            get { return (string)base["Tags"]; }
            set { base["Tags"] = value; }
        }
    }
}

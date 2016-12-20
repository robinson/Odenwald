using System;
using System.Collections.Generic;
using System.Configuration;

namespace Odenwald
{
    internal class OdenwaldConfigHelper
    {
        public static IDictionary<string, string> GetMetaData()
        {
            IDictionary<string, string> metaData = new Dictionary<string, string>();

            var coreConfig = ConfigurationManager.GetSection("OdenwaldConfig") as OdenwaldConfig;
            if (coreConfig == null)
            {
                throw new Exception("Cannot get configuration section : OdenwaldConfig");
            }
            foreach (OdenwaldConfig.TagConfig tagConfig in coreConfig.MetaData)
            {
                metaData[tagConfig.Name] = tagConfig.Value;
            }
            return (metaData);
        }
    }

    public class OdenwaldConfig : ConfigurationSection
    {
        [ConfigurationProperty("Settings", IsRequired = true)]
        public SettingsConfig Settings
        {
            get { return (SettingsConfig)base["Settings"]; }
            set { base["Settings"] = value; }
        }

        [ConfigurationProperty("Adapters", IsRequired = true)]
        [ConfigurationCollection(typeof(AdapterCollection), AddItemName = "Adapter")]
        public AdapterCollection Adapters
        {
            get { return (AdapterCollection)base["Adapters"]; }
            set { base["Adapters"] = value; }
        }

        [ConfigurationProperty("MetaData", IsRequired = false)]
        [ConfigurationCollection(typeof(TagCollection), AddItemName = "Tag")]
        public TagCollection MetaData
        {
            get { return (TagCollection)base["MetaData"]; }
            set { base["MetaData"] = value; }
        }

        public static OdenwaldConfig GetConfig()
        {
            return (OdenwaldConfig)ConfigurationManager.GetSection("OdenwaldConfig") ?? new OdenwaldConfig();
        }

        public sealed class SettingsConfig : ConfigurationElement
        {
            [ConfigurationProperty("ReadInterval", IsRequired = true)]
            public int ReadInterval
            {
                get { return (int)base["ReadInterval"]; }
                set { base["ReadInterval"] = value; }
            }
            [ConfigurationProperty("WriteInterval", IsRequired = true)]
            public int WriteInterval
            {
                get { return (int)base["WriteInterval"]; }
                set { base["WriteInterval"] = value; }
            }
            [ConfigurationProperty("Timeout", IsRequired = true)]
            public int Timeout
            {
                get { return (int)base["Timeout"]; }
                set { base["Timeout"] = value; }
            }

            [ConfigurationProperty("StoreRates", IsRequired = true)]
            public bool StoreRates
            {
                get { return (bool)base["StoreRates"]; }
                set { base["StoreRates"] = value; }
            }
        }

        public sealed class AdapterCollection : ConfigurationElementCollection
        {
            protected override ConfigurationElement CreateNewElement()
            {
                return new AdapterConfig();
            }

            protected override object GetElementKey(ConfigurationElement element)
            {
                return (((AdapterConfig)element).UniqueId);
            }
        }

        public sealed class AdapterConfig : ConfigurationElement
        {
            public AdapterConfig()
            {
                UniqueId = Guid.NewGuid();
            }

            internal Guid UniqueId { get; set; }

            [ConfigurationProperty("Name", IsRequired = true)]
            public string Name
            {
                get { return (string)base["Name"]; }
                set { base["Name"] = value; }
            }
            [ConfigurationProperty("Type", IsRequired = true)]
            public string Type
            {
                get { return (string)base["Type"]; }
                set { base["Type"] = value; }
            }

            [ConfigurationProperty("Class", IsRequired = true)]
            public string Class
            {
                get { return (string)base["Class"]; }
                set { base["Class"] = value; }
            }
            [ConfigurationProperty("Processor")]
            public string Processor
            {
                get { return (string)base["Processor"]; }
                set { base["Processor"] = value; }
            }
            [ConfigurationProperty("InputMetric")]
            public string InputMetric
            {
                get { return (string)base["InputMetric"]; }
                set { base["InputMetric"] = value; }
            }
            [ConfigurationProperty("OutputMetric")]
            public string OutputMetric
            {
                get { return (string)base["OutputMetric"]; }
                set { base["OutputMetric"] = value; }
            }

            [ConfigurationProperty("Enable", IsRequired = true)]
            public bool Enable
            {
                get { return (bool)base["Enable"]; }
                set { base["Enable"] = value; }
            }
        }

        public sealed class TagCollection : ConfigurationElementCollection
        {
            protected override ConfigurationElement CreateNewElement()
            {
                return new TagConfig();
            }

            protected override object GetElementKey(ConfigurationElement element)
            {
                return (((TagConfig)element).UniqueId);
            }
        }
        public sealed class TagConfig : ConfigurationElement
        {
            public TagConfig()
            {
                UniqueId = Guid.NewGuid();
            }

            internal Guid UniqueId { get; set; }

            [ConfigurationProperty("Name", IsRequired = true)]
            public string Name
            {
                get { return (string)base["Name"]; }
                set { base["Name"] = value; }
            }

            [ConfigurationProperty("Value", IsRequired = true)]
            public string Value
            {
                get { return (string)base["Value"]; }
                set { base["Value"] = value; }
            }
        }
    }
}

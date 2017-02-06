using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.KafkaProducerPlugin
{
    public class KafkaProducerPluginConfig : ConfigurationSection
    {
        public static KafkaProducerPluginConfig GetConfig()
        {
            return (KafkaProducerPluginConfig)ConfigurationManager.GetSection("KafkaProducer") ?? new KafkaProducerPluginConfig();
        }
        [ConfigurationProperty("Settings", IsRequired = true)]
        public SettingsConfig Settings
        {
            get { return (SettingsConfig)base["Settings"]; }
            set { base["Settings"] = value; }
        }
    }
    public sealed class SettingsConfig : ConfigurationElement
    {
        //[ConfigurationProperty("Interval", IsRequired = true)]
        //public int Interval
        //{
        //    get { return (int)base["Interval"]; }
        //    set { base["Interval"] = value; }
        //}
        [ConfigurationProperty("Url", IsRequired = true)]
        public string Url
        {
            get { return (string)base["Url"]; }
            set { base["Url"] = value; }
        }
        [ConfigurationProperty("Topic", IsRequired = true)]
        public string Topic
        {
            get { return (string)base["Topic"]; }
            set { base["Topic"] = value; }
        }
        [ConfigurationProperty("BatchDelayTime")]
        public int BatchDelayTime
        {
            get { return (int)base["BatchDelayTime"]; }
            set { base["BatchDelayTime"] = value; }
        }
        [ConfigurationProperty("BatchSize")]
        public int BatchSize
        {
            get { return (int)base["BatchSize"]; }
            set { base["BatchSize"] = value; }
        }
    }
}

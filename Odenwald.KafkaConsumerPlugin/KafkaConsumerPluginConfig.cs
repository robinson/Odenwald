using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.KafkaConsumerPlugin
{
    public class KafkaConsumerPluginConfig : ConfigurationSection
    {
        public static KafkaConsumerPluginConfig GetConfig()
        {
            return (KafkaConsumerPluginConfig)ConfigurationManager.GetSection("KafkaConsumer") ?? new KafkaConsumerPluginConfig();
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

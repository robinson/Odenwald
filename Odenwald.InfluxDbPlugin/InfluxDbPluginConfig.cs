using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.InfluxDbPlugin
{
    public sealed class InfluxDbPluginConfig : ConfigurationSection
    {
        public static InfluxDbPluginConfig GetConfig()
        {
            return (InfluxDbPluginConfig)ConfigurationManager.GetSection("InfluxDb") ?? new InfluxDbPluginConfig();
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
        [ConfigurationProperty("Interval", IsRequired = true)]
        public int Interval
        {
            get { return (int)base["Interval"]; }
            set { base["Interval"] = value; }
        }
        [ConfigurationProperty("Url", IsRequired = true)]
        public string Url
        {
            get { return (string)base["Url"]; }
            set { base["Url"] = value; }
        }
        [ConfigurationProperty("Database", IsRequired = true)]
        public string Database
        {
            get { return (string)base["Database"]; }
            set { base["Database"] = value; }
        }
        [ConfigurationProperty("Username")]
        public string Username
        {
            get { return (string)base["Username"]; }
            set { base["Username"] = value; }
        }
        [ConfigurationProperty("Password")]
        public string Password
        {
            get { return (string)base["Password"]; }
            set { base["Password"] = value; }
        }
    }
   
}

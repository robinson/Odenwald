
// ----------------------------------------------------------------------------
// Copyright (C) 2017 Robinson.
// https://github.com/robinson
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Odenwald
{
    internal class PluginRegistry
    {
        static ILog l_logger = LogManager.GetLogger(typeof(PluginRegistry));

        private readonly Dictionary<string, string> _registry = new Dictionary<string, string>();

        public PluginRegistry()
        {
            var config = ConfigurationManager.GetSection("Odenwald") as OdenwaldConfig;
            if (config == null)
            {
                l_logger.ErrorFormat("Cannot get configuration section : Odenwald");
                return;
            }
            foreach (
                OdenwaldConfig.PluginConfig pluginConfig in
                    config.Plugins.Cast<OdenwaldConfig.PluginConfig>()
                        .Where(pluginConfig => pluginConfig.Enable))
            {
                _registry[pluginConfig.Name] = pluginConfig.Class;
            }
        }

        public IList<IMetricsPlugin> CreatePlugins()
        {
            IList<IMetricsPlugin> plugins = new List<IMetricsPlugin>();
            foreach (var entry in _registry)
            {
                Type classType = Type.GetType(entry.Value);
                if (classType == null)
                {
                    l_logger.ErrorFormat("Cannot create plugin:{0}, class:{1}", entry.Key, entry.Value);
                    continue;
                }
                var plugin = (IMetricsPlugin)Activator.CreateInstance(classType);
                plugins.Add(plugin);
            }
            return (plugins);
        }
    }
}




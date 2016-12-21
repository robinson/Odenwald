using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald
{
    public class AdapterRegistry
    {
        static ILog l_logger = LogManager.GetLogger(typeof(AdapterRegistry));
        readonly Dictionary<string, string> _registry = new Dictionary<string, string>();
        readonly Dictionary<string, OdenwaldConfig.AdapterConfig> l_registry = new Dictionary<string, OdenwaldConfig.AdapterConfig>();
        public AdapterRegistry()
        {
            var config = ConfigurationManager.GetSection("OdenwaldConfig") as OdenwaldConfig;
            if (config == null)
            {
                l_logger.Error("Cannot get configuration section : OdenwaldConfig");
                return;
            }
            foreach (
                OdenwaldConfig.AdapterConfig adapterConfig in
                    config.Adapters.Cast<OdenwaldConfig.AdapterConfig>()
                        .Where(pluginConfig => pluginConfig.Enable))
            {
                _registry[adapterConfig.Name] = adapterConfig.Class;
                l_registry[adapterConfig.Name] = adapterConfig;
            }
        }
        public IList<IInputAdapter> CreateInputAdapter()
        {
            IList<IInputAdapter> adapters = new List<IInputAdapter>();
            foreach (var entry in l_registry.Where(t=>t.Value.Type == "input"))
            {
                Type classType = Type.GetType(entry.Value.Class);
                if (classType == null)
                {
                    l_logger.ErrorFormat("Cannot create adapter:{0}, class:{1}", entry.Key, entry.Value);
                    continue;
                }
                var adapter = (IInputAdapter)Activator.CreateInstance(classType);
                if ( adapter != null )
                {   
                    Type processorType = Type.GetType(entry.Value.Processor);
                    if (processorType == null)
                    {
                        l_logger.ErrorFormat("Cannot create processor:{0}, class:{1}", entry.Key, entry.Value);
                        continue;
                        
                        
                    }
                    var processor = (IInputProcessor)Activator.CreateInstance(processorType);
                    adapter.Processor = processor;

                    Type metricType = Type.GetType(entry.Value.InputMetric);

                    if (metricType == null)
                    {
                        l_logger.ErrorFormat("Cannot create input metric:{0}, class:{1}", entry.Key, entry.Value);
                        continue;
                    }
                    var metric = (IInputMetric)Activator.CreateInstance(metricType);
                    adapter.Processor.InputMetric = metric;

                    adapters.Add(adapter);
                }
            }
            return (adapters);
        }
        public IList<IOutputAdapter> CreateOutputAdapter()
        {
            IList<IOutputAdapter> adapters = new List<IOutputAdapter>();
            foreach (var entry in l_registry.Where(t => t.Value.Type == "output"))
            {
                Type classType = Type.GetType(entry.Value.Class);
                if (classType == null)
                {
                    l_logger.ErrorFormat("Cannot create adapter:{0}, class:{1}", entry.Key, entry.Value);
                    continue;
                }
                var adapter = (IAdapter)Activator.CreateInstance(classType);
                if (adapter != null)
                {
                    var outputAdapter = adapter as IOutputAdapter;
                    Type processorType = Type.GetType(entry.Value.Processor);
                    if (processorType == null)
                    {
                        l_logger.ErrorFormat("Cannot create processor:{0}, class:{1}", entry.Key, entry.Value);
                        continue;


                    }
                    var processor = (IOutputProcessor)Activator.CreateInstance(processorType);
                    outputAdapter.Processor = processor;

                    Type metricType = Type.GetType(entry.Value.OutputMetric);

                    if (metricType == null)
                    {
                        l_logger.ErrorFormat("Cannot create output metric:{0}, class:{1}", entry.Key, entry.Value);
                        continue;
                    }
                    var metric = (IOutputMetric)Activator.CreateInstance(metricType);
                    outputAdapter.Processor.OutputMetric = metric;

                    Type rawMetricType = Type.GetType(entry.Value.InputMetric);

                    if (rawMetricType == null)
                    {
                        l_logger.ErrorFormat("Cannot create output metric:{0}, class:{1}", entry.Key, entry.Value);
                        continue;
                    }
                    var rawMetric = (IInputMetric)Activator.CreateInstance(rawMetricType);
                    outputAdapter.Processor.RawMetric = rawMetric;

                    adapters.Add(outputAdapter);
                }
            }
            return (adapters);
        }
    }
}

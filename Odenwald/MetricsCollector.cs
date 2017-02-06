using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Odenwald
{
    public class MetricsCollector : IDisposable
    {
        #region attribute
        static ILog l_logger = LogManager.GetLogger(typeof(MetricsCollector));
        readonly ConcurrentQueue<MetricValue> l_metricQueue;
        private readonly Aggregator l_aggregator;
        const int l_maxQueueSize = 30000;
        readonly int l_interval;
        readonly IList<IMetricsPlugin> l_plugins;

        readonly int l_timeout;
        IDictionary<string, string> l_metaData;

        CancellationTokenSource l_readCancelToken;
        CancellationTokenSource l_writeCancelToken;
        CancellationTokenSource l_processCancelToken;

        EventWaitHandle l_eventWaitReadHandle = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString());
        EventWaitHandle l_eventWaitWriteHandle = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString());
        #endregion

        #region .ctor
        public MetricsCollector()
        {
            var config = ConfigurationManager.GetSection("OdenwaldConfig") as OdenwaldConfig;
            if (config == null)
            {
                l_logger.Error("Cannot get configuration section");
                return;
            }
            l_metaData = OdenwaldConfigHelper.GetMetaData();

            var registry = new PluginRegistry();
            l_plugins = registry.CreatePlugins();

            l_interval = config.GeneralSettings.Interval; //ms
            if (l_interval < 10)
                l_interval = 10;

            l_timeout = config.GeneralSettings.Timeout;
            if (l_timeout <= l_interval)
                l_timeout = l_interval * 300;
            var storeRates = config.GeneralSettings.StoreRates;
            l_aggregator = new Aggregator(l_timeout, storeRates);
            l_metricQueue = new ConcurrentQueue<MetricValue>();

            l_readCancelToken = new CancellationTokenSource();
            l_writeCancelToken = new CancellationTokenSource();
            l_processCancelToken = new CancellationTokenSource();
        }
        #endregion

        #region methods
        public void ConfigureAll()
        {
            l_logger.Debug("ConfigureAll() begin");
            //Parallel.ForEach(l_inputAdapters, ad => ad.Configure());
            //Parallel.ForEach(l_outputAdapters, ad => ad.Configure());
            foreach (var inputAdapter in l_plugins)
            {
                inputAdapter.Configure();
            }
        }
        public void StartAll()
        {
            l_logger.Debug("StartAll() begin");
            foreach (var inputAdapter in l_plugins)
            {
                inputAdapter.Start();
            }
            WriteOutput();
            ReadInput();
            RemoveExpired();
        }
        void ReadInput()
        {
            l_logger.Debug("Read() begin");
            int numDatasDropped = 0;

            var task = Task.Factory.StartNew((Func<Task>)(async () =>  // <- marked async
            {
                do
                {
                    l_eventWaitReadHandle.WaitOne(1);
                    try
                    {
                        foreach (IMetricsPlugin plugin in l_plugins)
                        {
                            var readPlugin = plugin as IMetricsReadPlugin;
                            if (readPlugin == null)
                            {
                                // skip if plugin is not a readplugin, it might be a writeplugin
                                continue;
                            }
                            IList<MetricValue> metricValues = readPlugin.Read();
                            if (metricValues == null || !metricValues.Any())
                                continue;
                            if (metricValues == null || !metricValues.Any())
                            {
                                l_logger.Debug("metric null!");
                                continue;
                            }
                            l_logger.DebugFormat("read with {0} metrics", metricValues.Count);

                            foreach (MetricValue metric in metricValues)
                            {
                                l_metricQueue.Enqueue(metric);
                                while (l_metricQueue.Count >= l_maxQueueSize)
                                {
                                    // When queue size grows above the Max limit, 
                                    // old entries are removed
                                    MetricValue removeMetric;
                                    l_metricQueue.TryDequeue(out removeMetric);
                                    if ((++numDatasDropped % 1000) == 0)
                                    {
                                        l_logger.ErrorFormat(
                                            "Number of data dropped : {0}",
                                            numDatasDropped);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        StackTrace st = new StackTrace(ex, true);

                        l_logger.ErrorFormat("ReadThreadProc() got exception : {0}, {1}", ex.ToString(), st.ToString());
                    }
                    
                    if (l_interval > 0)
                        await Task.Delay(l_interval, l_readCancelToken.Token);
                } while (true);               
            }), l_readCancelToken.Token);
        }

        void WriteOutput()
        {
            l_logger.Debug("Write() begin");
            bool needToFlush = false;
            var task = Task.Factory.StartNew((Func<Task>)(async () =>  // <- marked async
            {
                do
                {
                    l_eventWaitWriteHandle.WaitOne(1);
                    try
                    {
                        MetricValue metricValue = null; 

                        if (l_metricQueue.TryDequeue(out metricValue))
                        {
                            l_aggregator.Aggregate(ref metricValue);
                            metricValue.AddMetaData(l_metaData);
                            foreach (IMetricsPlugin plugin in l_plugins)
                            {
                                var writePlugin = plugin as IMetricsWritePlugin;
                                if (writePlugin == null)
                                {
                                    // skip if plugin is not a writeplugin, it might be a readplugin
                                    continue;
                                }
                                writePlugin.Write(metricValue);
                            }
                        }
                        if (needToFlush)
                        {
                            needToFlush = false;
                            foreach (IMetricsPlugin plugin in l_plugins)
                            {
                                var writePlugin = plugin as IMetricsWritePlugin;
                                if (writePlugin != null)
                                {
                                    // flush only if it is a Write plugin                    
                                    writePlugin.Flush();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        l_logger.ErrorFormat("Write() got exception : {0}", ex);
                    }
                    if (l_interval > 0)
                        await Task.Delay(0, l_writeCancelToken.Token); // <- await with cancellation, delay 1 secs
                } while (true);

            }), l_writeCancelToken.Token);
        }
        void RemoveExpired()
        {
            l_logger.Debug("RemoveExpired() begin");
            var task = Task.Factory.StartNew(async () =>  // <- marked async
            {
                while (true)
                {
                    l_aggregator.RemoveExpiredEntries();

                    await Task.Delay(TimeSpan.FromMilliseconds(l_timeout), l_writeCancelToken.Token); // <- await with cancellation, delay 1 secs
                }
            }, l_writeCancelToken.Token);


        }

        public void StopAll()
        {
            l_logger.Debug("StopAll() begin");
            l_readCancelToken.Cancel();
            l_writeCancelToken.Cancel();
            l_processCancelToken.Cancel();

            foreach (var plugin in l_plugins)
            {
                plugin.Stop();
            }
        }

        public void Dispose()
        {
            l_readCancelToken.Cancel();
            l_writeCancelToken.Cancel();
            l_processCancelToken.Cancel();
        }
        #endregion
    }



}

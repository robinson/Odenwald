using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Odenwald
{
    public class DataAcquisition : IDisposable
    {
        #region attribute
        static ILog l_logger = LogManager.GetLogger(typeof(DataAcquisition));
        readonly ConcurrentQueue<IInputMetric> l_metricQueue;
        const int l_maxQueueSize = 30000;
        readonly int l_readInterval;
        readonly int l_writeInterval;
        readonly IList<IInputAdapter> l_inputAdapters;
        readonly IList<IOutputAdapter> l_outputAdapters;
        readonly int l_timeout;
        IDictionary<string, string> l_metaData;

        CancellationTokenSource l_readCancelToken;
        CancellationTokenSource l_writeCancelToken;
        CancellationTokenSource l_processCancelToken;

        EventWaitHandle eventWaitReadHandle = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString());
        EventWaitHandle eventWaitWriteHandle = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString());
        #endregion

        #region .ctor
        public DataAcquisition()
        {
            var config = ConfigurationManager.GetSection("OdenwaldConfig") as OdenwaldConfig;
            if (config == null)
            {
                l_logger.Error("Cannot get configuration section");
                return;
            }
            l_metaData = OdenwaldConfigHelper.GetMetaData();
            var registry = new AdapterRegistry();

            l_inputAdapters = registry.CreateInputAdapter();
            l_outputAdapters = registry.CreateOutputAdapter();
            l_readInterval = config.Settings.ReadInterval;// > 2 ? config.Settings.ReadInterval : 2;
            l_writeInterval = config.Settings.WriteInterval;// > 2 ? config.Settings.ReadInterval : 2;
            l_timeout = config.Settings.Timeout > l_readInterval ? config.Settings.Timeout : 100;
            var storeRates = config.Settings.StoreRates;

            l_metricQueue = new ConcurrentQueue<IInputMetric>();

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
            foreach (var inputAdapter in l_inputAdapters)
            {
                inputAdapter.Configure();
            }
            foreach (var ouputAdapter in l_outputAdapters)
            {
                ouputAdapter.Configure();
            }
        }
        public void StartAll()
        {
            l_logger.Debug("StartAll() begin");
            foreach (var inputAdapter in l_inputAdapters)
            {
                inputAdapter.Start();
            }
            foreach (var ouputAdapter in l_outputAdapters)
            {
                ouputAdapter.Start();
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
                    eventWaitReadHandle.WaitOne(1);
                    for (int index = 0; index < l_inputAdapters.Count; index++)
                    {
                        var adapter = l_inputAdapters[index];
                        IList<IInputMetric> metrics = adapter.Read();

                        if (metrics == null || !metrics.Any())
                        {
                            l_logger.Debug("metric null!");
                            continue;
                        }
                        l_logger.DebugFormat("read from adapter {0} with {1} metrics", index, metrics.Count); 
                        for (int i = 0; i < metrics.Count; i++)
                        {
                            var metric = metrics[i];
                            adapter.Processor.ProcessInput(ref metric);
                            l_metricQueue.Enqueue(metric);
                            while (l_metricQueue.Count >= l_maxQueueSize)
                            {
                                // When queue size grows above the Max limit, 
                                // old entries are removed
                                IInputMetric inputMetric;
                                l_metricQueue.TryDequeue(out inputMetric);
                                if ((++numDatasDropped % 1000) == 0)
                                {
                                    l_logger.ErrorFormat(
                                        "Number of data dropped : {0}",
                                        numDatasDropped);
                                }
                            }
                        }
                    }
                    if (l_writeInterval > 0)
                        await Task.Delay(l_readInterval * 1000, l_readCancelToken.Token);
                } while (true);
                /*
                while (true)
                {
                    //Parallel.For(0, l_inputAdapters.Count, index =>
                    //{
                    //    var adapter = l_inputAdapters[index];
                    //    IList<IInputMetric> metrics = adapter.Read();
                    //    l_logger.DebugFormat("read from adapter {0}", index);
                    //    if (metrics == null || !metrics.Any())
                    //        continue;
                    //    Parallel.ForEach(metrics, m =>
                    //    {
                    //        adapter.Processor.ProcessInput(ref m);
                    //        l_metricQueue.Enqueue(m);
                    //        while (l_metricQueue.Count >= l_maxQueueSize)
                    //        {
                    //            // When queue size grows above the Max limit, 
                    //            // old entries are removed
                    //            IInputMetric inputMetric;
                    //            l_metricQueue.TryDequeue(out inputMetric);
                    //            if ((++numDatasDropped % 1000) == 0)
                    //            {
                    //                l_logger.ErrorFormat(
                    //                    "Number of data dropped : {0}",
                    //                    numDatasDropped);
                    //            }
                    //        }
                    //    });

                    //});
                    for (int index = 0; index < l_inputAdapters.Count; index++)
                    {
                        var adapter = l_inputAdapters[index];
                        IList<IInputMetric> metrics = adapter.Read();

                        if (metrics == null || !metrics.Any())
                        {
                            l_logger.Debug("metric null!");
                            continue;
                        }
                        else { l_logger.DebugFormat("read from adapter {0} with {1} metrics", index,metrics.Count); }
                        //Parallel.ForEach(metrics, m =>
                        //{
                        //    adapter.Processor.ProcessInput(ref m);
                        //    l_metricQueue.Enqueue(m);
                        //    while (l_metricQueue.Count >= l_maxQueueSize)
                        //    {
                        //        // When queue size grows above the Max limit, 
                        //        // old entries are removed
                        //        IInputMetric inputMetric;
                        //        l_metricQueue.TryDequeue(out inputMetric);
                        //        if ((++numDatasDropped % 1000) == 0)
                        //        {
                        //            l_logger.ErrorFormat(
                        //                "Number of data dropped : {0}",
                        //                numDatasDropped);
                        //        }
                        //    }
                        //});
                        for (int i = 0; i < metrics.Count; i++)
                        {
                            var metric = metrics[i];
                            adapter.Processor.ProcessInput(ref metric);
                            l_metricQueue.Enqueue(metric);
                            while (l_metricQueue.Count >= l_maxQueueSize)
                            {
                                // When queue size grows above the Max limit, 
                                // old entries are removed
                                IInputMetric inputMetric;
                                l_metricQueue.TryDequeue(out inputMetric);
                                if ((++numDatasDropped % 1000) == 0)
                                {
                                    l_logger.ErrorFormat(
                                        "Number of data dropped : {0}",
                                        numDatasDropped);
                                }
                            }
                        }
                    }
                    if (l_writeInterval > 0)
                        await Task.Delay(l_readInterval * 1000, l_readCancelToken.Token); // <- await with cancellation
                }*/
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
                    eventWaitWriteHandle.WaitOne(1);
                    try
                    {
                        IInputMetric inputMetric;

                        if (l_metricQueue.TryDequeue(out inputMetric))
                        {                            
                            //Parallel.For(0, l_outputAdapters.Count, index =>
                            //  {   
                            //      var adapter = l_outputAdapters[index];
                            //      if (inputMetric.GetType().IsAssignableFrom((Type)adapter.Processor.RawMetric.GetType()))
                            //      {
                            //          needToFlush = true;
                            //          adapter.Processor.ProcessInput(inputMetric);
                            //          adapter.Write(adapter.Processor.OutputMetric);
                            //          l_logger.DebugFormat("adapter {0} wrote", index);
                            //      }
                            //      if (index == l_outputAdapters.Count - 1)
                            //      {
                            //          //last adapter, not match with any metric then remove
                            //          l_metricQueue.TryDequeue(out inputMetric);
                            //      }
                            //  });
                            //Parallel.ForEach(l_outputAdapters, adapter =>
                            //{
                            //    if (inputMetric.GetType().IsAssignableFrom((Type)adapter.Processor.RawMetric.GetType()))
                            //    {
                            //        needToFlush = true;
                            //        inputMetric.Interval = l_readInterval;
                            //        inputMetric.AddMetaData(l_metaData);

                            //        adapter.Processor.ProcessInput(inputMetric);
                            //        adapter.Write(adapter.Processor.OutputMetric);
                            //        //l_logger.DebugFormat("adapter {0} wrote", adapter);//TODO: give adapter a name, for debug purpose
                            //    }
                            //    if (i == l_outputAdapters.Count - 1)
                            //    {
                            //        //last adapter, not match with any metric then remove
                            //        l_metricQueue.TryDequeue(out inputMetric);
                            //    }
                            //});
                            inputMetric.Interval = l_readInterval;
                            inputMetric.AddMetaData(l_metaData);
                            for (int i = 0; i < l_outputAdapters.Count; i++)
                            {
                                var adapter = l_outputAdapters[i];
                                if (inputMetric.GetType().IsAssignableFrom((Type)adapter.Processor.RawMetric.GetType()))
                                {
                                    needToFlush = true;
                                    adapter.Processor.ProcessInput(inputMetric);
                                    adapter.Write(adapter.Processor.OutputMetric);
                                    l_logger.DebugFormat("adapter {0} wrote", i);
                                }
                                if (i == l_outputAdapters.Count - 1)
                                {
                                    //last adapter, not match with any metric then remove
                                    l_metricQueue.TryDequeue(out inputMetric);
                                }
                            }
                        }
                        if (needToFlush)
                        {
                            needToFlush = false;
                            foreach (IOutputAdapter adapter in l_outputAdapters)
                            {
                                adapter.Flush();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        l_logger.ErrorFormat("Write() got exception : {0}", ex);
                    }
                    if (l_writeInterval > 0)
                        await Task.Delay(l_writeInterval * 1000, l_writeCancelToken.Token); // <- await with cancellation, delay 1 secs
                } while (true);
               
            }), l_writeCancelToken.Token);
        }
        void RemoveExpired()
        {
            //nothing to remove
            //l_logger.Debug("RemoveExpired() begin");
            //var task = Task.Factory.StartNew(async () =>  // <- marked async
            //{
            //    while (true)
            //    {


            //        await Task.Delay(TimeSpan.FromMilliseconds(l_timeout *1000), l_writeCancelToken.Token); // <- await with cancellation, delay 1 secs
            //    }
            //}, l_writeCancelToken.Token);


        }

        public void StopAll()
        {
            l_logger.Debug("StopAll() begin");
            l_readCancelToken.Cancel();
            l_writeCancelToken.Cancel();
            l_processCancelToken.Cancel();

            foreach (var inputAdapter in l_inputAdapters)
            {
                inputAdapter.Stop();
            }
            foreach (var ouputAdapter in l_outputAdapters)
            {
                ouputAdapter.Stop();
            }
            //Parallel.ForEach(l_inputAdapters, ad => ad.Stop());
            //Parallel.ForEach(l_outputAdapters, ad => ad.Stop());
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

using log4net;
using Odenwald.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald
{
    internal class CacheEntry
    {
        public MetricValue MetricRate;
        public double[] RawValues;

        public CacheEntry(MetricValue metricValue)
        {
            MetricRate = metricValue;
            RawValues = (double[])metricValue.Values.Clone();
        }
    }
    public class Aggregator
    {
        static ILog l_logger = LogManager.GetLogger(typeof(Aggregator));
        readonly Dictionary<string, CacheEntry> l_cache;
        readonly Object l_cacheLock;
        readonly bool l_storeRates;
        readonly int l_timeoutSeconds;
        public Aggregator(int timeoutSeconds, bool storeRates)
        {
            l_cache = new Dictionary<string, CacheEntry>();
            l_cacheLock = new object();
            l_timeoutSeconds = timeoutSeconds;
            l_storeRates = storeRates;
        }
       

     

        public void Aggregate(ref MetricValue metric)
        {
            // If rates are not stored then there is nothing to aggregate
            if (!l_storeRates)
            {
                return;
            }
            IList<DataSource> dsl = DataSetCollection.Instance.GetDataSource(metric.TypeName);
            if (dsl == null || metric.Values.Length != dsl.Count)
            {
                return;
            }
            double now = Util.GetNow();
            lock (l_cacheLock)
            {
                CacheEntry cEntry;
                string key = metric.Key;

                if (!(l_cache.TryGetValue(key, out cEntry)))
                {
                    cEntry = new CacheEntry(metric.DeepCopy());
                    for (int i = 0; i < metric.Values.Length; i++)
                    {
                        if (dsl[i].Type == DsType.Derive ||
                            dsl[i].Type == DsType.Absolute ||
                            dsl[i].Type == DsType.Counter)
                        {
                            metric.Values[i] = double.NaN;
                            cEntry.MetricRate.Values[i] = double.NaN;
                        }
                    }
                    l_cache[key] = cEntry;
                    return;
                }
                for (int i = 0; i < metric.Values.Length; i++)
                {
                    double rawValNew = metric.Values[i];
                    double rawValOld = cEntry.RawValues[i];
                    double rawValDiff = rawValNew - rawValOld;
                    double timeDiff = cEntry.MetricRate.Epoch - DateTimeOffset.Now.ToUnixTimeSeconds();

                    double rateNew = rawValDiff / timeDiff;

                    switch (dsl[i].Type)
                    {
                        case DsType.Gauge:
                            // no rates calculations are done, values are stored as-is for gauge
                            cEntry.RawValues[i] = rawValNew;
                            cEntry.MetricRate.Values[i] = rawValNew;
                            break;

                        case DsType.Absolute:
                            // similar to gauge, except value will be divided by time diff
                            cEntry.MetricRate.Values[i] = rawValNew / timeDiff;
                            cEntry.RawValues[i] = rawValNew;
                            metric.Values[i] = cEntry.MetricRate.Values[i];
                            break;

                        case DsType.Derive:
                            cEntry.RawValues[i] = rawValNew;
                            cEntry.MetricRate.Values[i] = rateNew;
                            metric.Values[i] = rateNew;

                            break;

                        case DsType.Counter:
                            // Counters are very simlar to derive except when counter wraps around                                
                            if (rawValNew < rawValOld)
                            {
                                // counter has wrapped around
                                cEntry.MetricRate.Values[i] = rawValNew / timeDiff;
                                cEntry.RawValues[i] = rawValNew;
                                metric.Values[i] = cEntry.MetricRate.Values[i];
                            }
                            else
                            {
                                cEntry.MetricRate.Values[i] = rateNew;
                                cEntry.RawValues[i] = rawValNew;
                                metric.Values[i] = rateNew;
                            }
                            break;
                    }
                    // range checks
                    if (Convert.ToDouble(metric.Values[i]) < dsl[i].Min)
                    {
                        metric.Values[i] = dsl[i].Min;
                        cEntry.RawValues[i] = metric.Values[i];
                    }
                    if (Convert.ToDouble(metric.Values[i]) > dsl[i].Max)
                    {
                        metric.Values[i] = dsl[i].Max;
                        cEntry.RawValues[i] = metric.Values[i];
                    }
                }
            }
        }

        public void RemoveExpiredEntries()
        {//TODO: consider about remove expired entries in the cache
         // If rates are not stored then there is nothing to remove
            if (!l_storeRates)
            {
                return;
            }
            double now = Util.GetNow();
            double expirationTime = now - l_timeoutSeconds;
            var removeList = new List<string>();

            lock (l_cacheLock)
            {
                removeList.AddRange(from pair in l_cache
                                    let cEntry = pair.Value
                                    where cEntry.MetricRate.Epoch < expirationTime
                                    select pair.Key);
                if (removeList.Count > 0)
                   l_logger.DebugFormat("Removing expired entries: {0}", removeList.Count);
                foreach (string key in removeList)
                {
                    l_cache.Remove(key);
                }
            }
        }
    }
}

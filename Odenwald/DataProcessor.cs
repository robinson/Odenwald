using log4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Odenwald
{
    internal class CacheEntry
    {
        public DataValue MetricRate;
        public object[] RawValues;

        public CacheEntry(DataValue metricValue)
        {
            MetricRate = metricValue;
            RawValues = (object[])metricValue.Values.Clone();
        }
    }

    public class DataProcessor
    {
        static ILog l_logger = LogManager.GetLogger(typeof(DataProcessor));
        readonly Dictionary<string, CacheEntry> l_cache;
        readonly Object l_cacheLock;
        readonly bool l_storeRates;
        readonly int l_timeoutSeconds;
        public DataProcessor(int timeoutSeconds, bool storeRates)
        {
            l_cache = new Dictionary<string, CacheEntry>();
            l_cacheLock = new object();
            l_timeoutSeconds = timeoutSeconds;
            l_storeRates = storeRates;
        }
        public void Process(ref DataValue dataValue)
        {
            // If rates are not stored then there is nothing to aggregate
            if (!l_storeRates)
            {
                return;
            }
            IList<DataSource> dsl = DataSetCollection.Instance.GetDataSource(dataValue.TypeName);
            if (dsl == null || dataValue.Values.Length != dsl.Count)
            {
                return;
            }

            double now = Util.GetNow();

            lock (l_cacheLock)
            {
                CacheEntry cEntry;
                string key = dataValue.Key();

                if (!(l_cache.TryGetValue(key, out cEntry)))
                {
                    cEntry = new CacheEntry(dataValue.Clone());
                    for (int i = 0; i < dataValue.Values.Length; i++)
                    {
                        if (dsl[i].Type == DsType.Derive ||
                            dsl[i].Type == DsType.Absolute ||
                            dsl[i].Type == DsType.Counter)
                        {
                            dataValue.Values[i] = double.NaN;
                            cEntry.MetricRate.Values[i] = double.NaN;
                        }
                    }
                    cEntry.MetricRate.Epoch = now;
                    l_cache[key] = cEntry;
                    return;
                }
                for (int i = 0; i < dataValue.Values.Length; i++)
                {
                    double rawValNew;
                    double rawValOld;

                    if (double.TryParse(dataValue.Values[i].ToString(), out rawValNew) && double.TryParse(cEntry.RawValues[i].ToString(), out rawValOld))
                    {
                        //process number
                        double rawValDiff = rawValNew - rawValOld;
                        double timeDiff = cEntry.MetricRate.Epoch - now;

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
                                dataValue.Values[i] = cEntry.MetricRate.Values[i];
                                break;

                            case DsType.Derive:
                                cEntry.RawValues[i] = rawValNew;
                                cEntry.MetricRate.Values[i] = rateNew;
                                dataValue.Values[i] = rateNew;

                                break;

                            case DsType.Counter:
                                // Counters are very simlar to derive except when counter wraps around                                
                                if (rawValNew < rawValOld)
                                {
                                    // counter has wrapped around
                                    cEntry.MetricRate.Values[i] = rawValNew / timeDiff;
                                    cEntry.RawValues[i] = rawValNew;
                                    dataValue.Values[i] = cEntry.MetricRate.Values[i];
                                }
                                else
                                {
                                    cEntry.MetricRate.Values[i] = rateNew;
                                    cEntry.RawValues[i] = rawValNew;
                                    dataValue.Values[i] = rateNew;
                                }
                                break;
                        }
                        // range checks
                        if (Convert.ToDouble(dataValue.Values[i]) < dsl[i].Min)
                        {
                            dataValue.Values[i] = dsl[i].Min;
                            cEntry.RawValues[i] = dataValue.Values[i];
                        }
                        if (Convert.ToDouble(dataValue.Values[i]) > dsl[i].Max)
                        {
                            dataValue.Values[i] = dsl[i].Max;
                            cEntry.RawValues[i] = dataValue.Values[i];
                        }
                    }
                    else
                    {
                        //process object, nur Gauge
                        cEntry.RawValues[i] = dataValue.Values[i];
                        cEntry.MetricRate.Values[i] = dataValue.Values[i];
                    }

                    cEntry.MetricRate.Epoch = now;
                    l_cache[key] = cEntry;
                }
            }

        }

        public void RemoveExpiredEntries()
        {
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



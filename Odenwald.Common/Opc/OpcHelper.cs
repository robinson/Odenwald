using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.Common.Opc
{
    public static class OpcHelper
    {
        static ILog l_logger = LogManager.GetLogger(typeof(OpcHelper));
        public static bool QualifyMetric(ref OpcMetric metric, Dictionary<string, object> cache )
        {
            if (PointHasGoodOrDifferentBadStatus(metric))
            {
                if (!cache.ContainsKey(metric.Measurement.Name))
                {
                    cache.Add(metric.Measurement.Name, metric.OpcValue);
                    metric.Measurement.LastOpcstatus = metric.Opcstatus;
                    metric.Measurement.LastValue = metric.OpcValue;
                }
                else
                {
                    object lastvalue;
                    cache.TryGetValue(metric.Measurement.Name, out lastvalue);
                    metric.Measurement.LastValue = lastvalue;
                }


                if (!PointIsValid(metric) || !PointMatchesType(metric))
                {
                    // Set de default value for the type specified
                    l_logger.ErrorFormat("Invalid point: measurement.name<{0}>, measurement.path<{1}>, metric.value<{2}>", metric.Measurement.Name, metric.Measurement.Path, metric.OpcValue.ToString());
                    switch (metric.Measurement.DataType)
                    {
                        case "number":
                            metric.OpcValue = 0;
                            break;
                        case "boolean":
                            metric.OpcValue = false;
                            break;
                        case "string":
                            metric.OpcValue = "";
                            break;
                        default:
                            l_logger.Error("No valid datatype, ignoring point");
                            break;
                    }
                    return false;
                }
            }
            // Check for deadband
            if (PointIsWithinDeadband(metric)) return false;
            if (!PointMatchesType(metric))
            {
                l_logger.ErrorFormat("Invalid type returned from OPC. Ignoring point {0}", metric.Measurement.Name);
                return false;
            }
            return true;
        }

        static bool PointMatchesType(OpcMetric metric)
        {
            var match = (metric.OpcValue.IsNumber() && metric.Measurement.DataType == "number")
                || metric.OpcValue.IsBoolean() && metric.Measurement.DataType == "boolean"
                || metric.OpcValue.IsString() && metric.Measurement.DataType == "string";

            if (!match)
            {
                //log here

            }
            return match;
        }
        static bool PointIsValid(OpcMetric p)
        {
            // check if the value is a type that we can handle (number or a bool).
            return (p.OpcValue != null && (p.OpcValue.IsBoolean() || p.OpcValue.IsNumber())) || p.OpcValue.IsString();

        }
        static bool PointHasGoodOrDifferentBadStatus(OpcMetric p)
        {
            var curr = p.Opcstatus;
            var prev = p.Measurement.LastOpcstatus;

            if (curr == "Good" || curr != prev || curr == "S_OK") return true;
            return false;
        }


        static bool PointIsWithinDeadband(OpcMetric metric)
        {
            // some vars for shorter statements later on.
            var curr = metric.OpcValue;
            var prev = metric.Measurement.LastValue;
            var dba = metric.Measurement.DeadbandAbsolute;
            var dbr = metric.Measurement.DeadbandRelative;

            // return early if the type of the previous value is not the same as the current.
            // this will also return when this is the first value and prev is still undefined.
            if (curr.GetType() != prev.GetType()) return false;

            // calculate deadbands based on value type. For numbers, make the
            // calculations for both absolute and relative if they are set. For bool,
            // just check if a deadband has been set and if the value has changed.

            if (curr.IsNumber())
            {

                if (dba > 0 && Math.Abs(System.Convert.ToDecimal(curr) - System.Convert.ToDecimal(prev)) < dba)
                {
                    return true;
                }
                if (dbr > 0 && Math.Abs(System.Convert.ToDecimal(curr) - System.Convert.ToDecimal(prev)) < Math.Abs(System.Convert.ToDecimal(prev)) * dbr)
                {
                    // console.log("New value is within relative deadband.", p);
                    return true;

                }
            }
            else if (curr.IsBoolean())
            {
                if (dba > 0 && prev == curr)
                    // console.log("New value is within bool deadband.", p);
                    return true;

            }
            else if (curr.IsString())
                return true;
            return false;

        }
    }
}

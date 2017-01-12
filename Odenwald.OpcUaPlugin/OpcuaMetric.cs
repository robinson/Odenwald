using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Odenwald.OpcUaPlugin
{
    public class OpcUaMetric
    {
        #region Attribute
        static ILog l_logger = LogManager.GetLogger(typeof(OpcUaMetric));
        private readonly IDictionary<string, string> l_meta = new SortedDictionary<string, string>();
        #endregion


        public object OpcValue { get; set; }
        public MeasurementDto Measurement { get; set; }

        public DateTime Timestamp { get; set; }
        public string Opcstatus { get; set; }

        public string JsonString()
        {
            object opcUaObject = new
            {
                OpcValue = OpcValue,
                Timestamp = Timestamp,
                Opcstatus = Opcstatus,
                Name = Measurement.Name,
                DataType = Measurement.DataType,
                AttributeId = Measurement.AttributeId,
                Path = Measurement.Path,
                DeadbandAbsolute = Measurement.DeadbandAbsolute,
                DeadbandRelative = Measurement.DeadbandRelative,
                LastValue = Measurement.LastValue,
                LastOpcstatus = Measurement.LastOpcstatus
            };
            return (JsonConvert.SerializeObject(opcUaObject));
        }
    }
}

using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.Common
{
    public static class Util
    {
        static ILog l_logger = LogManager.GetLogger(typeof(Util));

        public static double GetNow()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            double epoch = t.TotalMilliseconds / 1000;
            double now = Math.Round(epoch, 3);
            return (now);
        }

        public static string GetHostName()
        {
            string hostname = Environment.MachineName.ToLower();
            try
            {
                hostname = Dns.GetHostEntry("localhost").HostName.ToLower();
            }
            catch (SocketException)
            {
                l_logger.WarnFormat("Unable to resolve hostname, using MachineName: {0}", hostname);
            }
            return hostname;
        }
        public static bool IsNumber(this object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }
        public static bool IsBoolean(this object value)
        {
            return value is bool;
        }
        public static bool IsString(this object value)
        {
            return value is string;
        }

    }
}

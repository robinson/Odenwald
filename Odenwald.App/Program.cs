using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.App
{
    public static class Program
    {
        static ILog l_logger = LogManager.GetLogger(typeof(Program));
        public static void Main(string[] args)
        {
            
            var config = ConfigurationManager.GetSection("Odenwald") as OdenwaldConfig;
            if (config == null)
            {
                l_logger.Error("Main(): cannot get configuration section");
                return;
            }
            var odenWaldService = new OdenwaldService();

            if (Array.Find(args, s => s.Equals(@"app")) != null)
            {
                Console.WriteLine("*** Starting OdenwaldApp in console mode***");
                // run as a console application for testing and debugging purpose
                odenWaldService.StartService();
                Console.WriteLine("*** Enter Ctrl-C to exit: ***");
                Console.ReadLine();
                odenWaldService.StopService();
            }
            else
            {
                // run as a windows service
                ServiceBase[] servicesToRun = { odenWaldService };
                ServiceBase.Run(servicesToRun);
            }
            l_logger.Error("OdenwaldService: exiting ...");
        }


    }
}

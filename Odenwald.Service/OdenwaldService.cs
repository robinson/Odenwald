using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.Runner
{
    partial class OdenwaldService : ServiceBase
    {
        static ILog l_logger = LogManager.GetLogger(typeof(OdenwaldService));
        private MetricsCollector l_metricCollector;
        public OdenwaldService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            StartService(args);
        }

        protected override void OnStop()
        {
            StopService();
        }
        public virtual void StartService(params string[] args)
        {
            l_logger.Debug("StartService() begin");
            l_metricCollector = new MetricsCollector();
            l_metricCollector.ConfigureAll();
            l_metricCollector.StartAll();
            l_logger.Debug("StartService() return");
        }

        // public accessibility for running as a console application
        public virtual void StopService()
        {
            l_logger.Debug("StopService() begin");
            l_metricCollector.StopAll();
            l_logger.Debug("StopService() return");
        }
    }
}

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
        private DataAcquisition l_dataAcquisition;
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
            l_dataAcquisition = new DataAcquisition();
            l_dataAcquisition.ConfigureAll();
            l_dataAcquisition.StartAll();
            l_logger.Debug("StartService() return");
        }

        // public accessibility for running as a console application
        public virtual void StopService()
        {
            l_logger.Debug("StopService() begin");
            l_dataAcquisition.StopAll();
            l_logger.Debug("StopService() return");
        }
    }
}

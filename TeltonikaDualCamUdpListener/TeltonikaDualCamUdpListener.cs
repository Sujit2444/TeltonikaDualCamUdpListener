using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using TeltonikaDualCamUdpNetBase.BaseClasses;
using TeltonikaDualCamUdpNetBase.MessageProcessors;

namespace TeltonikaDualCamUdpListener
{
    public partial class TeltonikaDualCamUdpListener : ServiceBase
    {
        private ListenerRunner _listenerRunner;
        public TeltonikaDualCamUdpListener()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Utils.LogToFile(6, "[INFO]", "Service Start");
            IMessageProcessor messageProcessor = new TeltonikaMessageProcessor();
            int port = Convert.ToInt32(ConfigurationManager.AppSettings["TeltonikaPort"]);
            _listenerRunner = new ListenerRunner(port, messageProcessor);
            _listenerRunner.Start();
        }

        protected override void OnStop()
        {
            Utils.LogToFile(6, "[INFO]","Service Stop");
            if (_listenerRunner != null)
            _listenerRunner.Stop();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TeltonikaDualCamUdpNetBase.BaseClasses;
using TeltonikaDualCamUdpNetBase.MessageProcessors;

namespace TeltonikaDualCamUdpListenerForm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            Utils.LogToFile(6,"[INFO]","Calling Form1()");
            InitializeComponent();

            StartLMUMessageProcessor();
        }

        private void StartLMUMessageProcessor()
        {
            Utils.LogToFile(6, "[INFO]", "Calling StartLMUMessageProcessor()");
            IMessageProcessor messageProcessor = new TeltonikaMessageProcessor();
            int port = Convert.ToInt32(ConfigurationManager.AppSettings["TeltonikaPort"]);
            ListenerRunner listenerRunner = new ListenerRunner(port,messageProcessor);
            listenerRunner.Start();
        }

    }
}

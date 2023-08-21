using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using TeltonikaDualCamUdpNetBase.BaseClasses;

namespace TeltonikaDualCamUdpListener
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            Utils.LogToFile(6, "[INFO]", "Calling Main()");
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new TeltonikaDualCamUdpListener()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}

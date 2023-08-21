using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeltonikaDualCamUdpNetBase.BaseClasses
{
    public static class Utils
    {
        private static int _logDetailLevel;
        private static ILog _log;

        static Utils()
        {
             log4net.Config.XmlConfigurator.Configure();
            _logDetailLevel = string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["LogDetailLevel"]) ? 3 : int.Parse(ConfigurationManager.AppSettings["LogDetailLevel"]);
            _log = log4net.LogManager.GetLogger("TeltonikaDualCamUdpListenerLog");                 
        }

        public static void LogToFile(int detailLevel,string messageType,string message)
        {
            if (detailLevel > _logDetailLevel)
                return;

            _log.Info($"Time:{DateTime.Now.ToString("yyyy-mm-dd hh:mm:ss:fff tt")} | {messageType} | {message}");

        }

        public static int GetListenerId()
        {
            if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["ListenerId"]))
            {
                int listenerId = int.Parse(ConfigurationManager.AppSettings["ListenerId"]);
                return listenerId;
            }
            return 0;
        }



    }
}

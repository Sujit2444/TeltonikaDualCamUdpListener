using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeltonikaDualCamUdpNetBase.BaseClasses
{
    public class ListenerRunner
    {
        private DatabaseSaver _databaseSaver;
        private UdpServer _udpServer;
        private Thread _listenerThread;
        private Thread _dbThread;
        private Thread _messageprocessorThread;
        private int _port;
        private IMessageProcessor _messageProcessor;

        private void CreateAll()
        {
            Utils.LogToFile(6, "[INFO]", "Calling CreateAll()");
            _databaseSaver = new DatabaseSaver();
            if (_messageProcessor != null)
                _messageProcessor.SetDbSaver(_databaseSaver);

            _udpServer = new UdpServer(_port, _messageProcessor);
            _listenerThread = new Thread(_udpServer.Run);
            _listenerThread.IsBackground = true;
            _messageprocessorThread = new Thread(_messageProcessor.RunBulk);
            _messageprocessorThread.IsBackground = true;
            
        }

        public ListenerRunner(int port,IMessageProcessor messageProcessor)
        {
            Utils.LogToFile(6, "[INFO]", "Calling ListenerRunner()");
            _port = port;
            _messageProcessor = messageProcessor;
            CreateAll();
        }

        public void Start()
        {
            Utils.LogToFile(6, "[INFO]", "Calling Start()");
            _listenerThread.Start();
            _messageprocessorThread.Start();
        }

        public void Stop()
        {
            Utils.LogToFile(6, "[INFO]", "Calling Stop()");
            _listenerThread.Abort();
            _messageProcessor.Stopped = true;
            _messageprocessorThread.Abort();
            Thread.Sleep(100);
        }
    }
}

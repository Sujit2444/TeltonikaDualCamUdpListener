using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TeltonikaDualCamUdpNetBase.MessageProcessors;

namespace TeltonikaDualCamUdpNetBase.BaseClasses
{
    public class UdpServer
    {
        private int _port = 0;
        private IMessageProcessor _messageProcessor = null;
        public UdpServer(int port,IMessageProcessor messageProcessor)
        {
            Utils.LogToFile(6, "[INFO]", "Calling UdpServer()");
            _port = port;
            _messageProcessor = messageProcessor;
        }


        private void InitMessageProcesing(UdpClient client,IPAddress ipAddress)
        {
             Utils.LogToFile(6, "[INFO]", "Calling InitMessageProcesing()");
            _messageProcessor.Client = client;
            _messageProcessor.NoAckForIP = ipAddress;
        }

        private void AddProcessMessageQueue(UdpState udpState)
        {
            Utils.LogToFile(6, "[INFO]", "Calling AddProcessMessageQueue()");
            _messageProcessor.Add(udpState);
        }
        public void Run()
        {
            Utils.LogToFile(6, "[INFO]", "Calling  Run()");
            IPAddress NoAckForIp = null;
            if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["NoAckForIpGprs"]))
            {
                NoAckForIp = IPAddress.Parse(ConfigurationManager.AppSettings["NoAckForIpGprs"]);
            }
            Utils.LogToFile(6, "[INFO]", $"NoAckForIp:{ NoAckForIp }");
            string teltonikaIpString = ConfigurationManager.AppSettings["TeltonikaIpAddressForGprs"];
            Utils.LogToFile(6, "[INFO]", $"Teltonika Server Ip:{ teltonikaIpString }");
            Utils.LogToFile(6, "[INFO]", $"Teltonika Server Port:{_port}");
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(teltonikaIpString),_port);
            UdpClient udpClient = new UdpClient();
            udpClient.ExclusiveAddressUse = false;
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress,true);
            udpClient.Client.Bind(iPEndPoint);
            udpClient.DontFragment = true;
            
            UdpClient ackClient = new UdpClient();
            ackClient.ExclusiveAddressUse = false;
            ackClient.Client.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress,true);
            ackClient.Client.Bind(iPEndPoint);
            ackClient.DontFragment = true;
         
            InitMessageProcesing(ackClient, NoAckForIp);

            while (true)
            {
                try
                {
                    IPEndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
                    Utils.LogToFile(6, "[INFO]", "Service Running");
                    byte[] data = udpClient.Receive(ref remoteIp);

                    if (data == null)
                        continue;
                    Utils.LogToFile(6, "[INFO]", $"Raw Data:{BitConverter.ToString(data)}");
                    AddProcessMessageQueue(new UdpState {Data=data,Remote=remoteIp,Port=_port});

                }
                catch (Exception ex)
                {
                    Utils.LogToFile(1, "[EXCEPTION]", $"Exception In Run():{ex.Message.ToString()}");
                }           

            }

        }

    }
}

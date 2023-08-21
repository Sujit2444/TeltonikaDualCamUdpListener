using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TeltonikaDualCamUdpNetBase.BaseClasses
{
    public interface IMessageProcessor
    {
       UdpClient Client { get; set; }
       IPAddress NoAckForIP { get; set; }
       void Add(MessageProcessors.UdpState udpState);
       bool Stopped { get; set; }
       void RunBulk();
       void SetDbSaver(DatabaseSaver databaseSaver);
    }
}

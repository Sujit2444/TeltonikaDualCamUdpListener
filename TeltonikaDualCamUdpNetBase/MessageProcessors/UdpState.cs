using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TeltonikaDualCamUdpNetBase.MessageProcessors
{
   public class UdpState
    {
        public byte[] Data { get; set; }
        public IPEndPoint Remote { get; set; }
        public int Port { get; set; }
    }
}

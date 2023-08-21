using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Teltonika.Codec;
using Teltonika.Codec.Model;
using TeltonikaDualCamUdpNetBase.BaseClasses;

namespace TeltonikaDualCamUdpNetBase.MessageProcessors
{
   public class TeltonikaMessageProcessor: IMessageProcessor
    {
        private bool _stopped;
        public bool  Stopped{ get { return _stopped; } set { _stopped = value; } }
        private UdpClient _client;
        public UdpClient Client { get { return _client; } set { _client = value; } }
        private IPAddress _noAckForIP;
        public IPAddress NoAckForIP { get { return _noAckForIP; } set { _noAckForIP = value; } }
        private object _udpStateQueueLock = new object();
        private Queue<UdpState> _udpStateQueue = new Queue<UdpState>();
        private DatabaseSaver _dbSaver = null;

        public void Add(UdpState udpState)
        {
            Utils.LogToFile(6, "[INFO]", "Calling Add()");
            lock (_udpStateQueueLock)
            {
                _udpStateQueue.Enqueue(udpState);
            }
        }

        public void RunBulk()
        {
            Utils.LogToFile(3, "[INFO]", "Calling RunBulk()");
            Stopped = false;
            while (!Stopped)
            {
                try
                {
                    List<UdpState> udpStateList = new List<UdpState>();
                    List<UdpDataPacket> teltonikaMessageList = new List<UdpDataPacket>();

                    lock(_udpStateQueueLock)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            if (_udpStateQueue.Count > 0)
                                udpStateList.Add(_udpStateQueue.Dequeue());
                            else
                                break;
                        }
                    }

                    if (udpStateList.Count>0)
                    {
                        try
                        {
                            StringBuilder bulkInsertSql = new StringBuilder();
                            bulkInsertSql.Append("DECLARE @ErrorID TABLE (Id INT)");
                            int udpIndex = 0;
                            foreach (var udp in udpStateList)
                            {
                                try
                                {
                                    Utils.LogToFile(3, "[INFO]", $"Bulk Processing: {BitConverter.ToString(udp.Data)}");
                                    UdpDataPacket udpDataPacket = RawDeserialize(udp.Data);
                                    teltonikaMessageList.Add(udpDataPacket);
                                    string imeiNumber = System.Text.Encoding.Default.GetString(udpDataPacket.Imei);
                                    foreach (var data in udpDataPacket.AvlData.Data)
                                    {
                                        string sql = ParseToSQL(udp.Remote.Address.ToString(),imeiNumber,data);

                                        bulkInsertSql.AppendLine("BEGIN TRY");
                                        bulkInsertSql.AppendLine(sql);
                                        bulkInsertSql.AppendLine("END TRY");
                                        bulkInsertSql.AppendLine("BEGIN CATCH");
                                        bulkInsertSql.AppendLine("INSERT  @ErrorID SELECT " + udpIndex);
                                        bulkInsertSql.AppendLine("END CATCH");
                                        udpIndex++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Utils.LogToFile(1, "[EXCEPTION]", $"Exception In RunBulk() RawDeserialize Error:{ex.Message.ToString()}");
                                }

                                //udpIndex++;  //<=check it
                            }
                            bulkInsertSql.AppendLine("SELECT * FROM @ErrorID");
                            DateTime dbSaveStartTime = DateTime.Now;
                            //code comment
                            List<int> errorIds = _dbSaver.BulkWriteToDatabase(bulkInsertSql.ToString());
                            Utils.LogToFile(0, "[INFO]", $"BulkWriteToDatabase(Number Of Row Inserted,Duration Of The Operation) = ({udpIndex}, {DateTime.Now.Subtract(dbSaveStartTime).ToString()})");
                            foreach (var id in errorIds)
                            {
                                Utils.LogToFile(1, "[INFO]", $"DB Save Error In RunBulk():{BitConverter.ToString(udpStateList[id].Data)}");
                            }
                            //
                            int udpStateListIndex = 0;
                            foreach (var dataPacket in teltonikaMessageList)
                            {
                                IPEndPoint remote = udpStateList[udpStateListIndex].Remote;
                                byte[] inputData = udpStateList[udpStateListIndex].Data;
                                try
                                {
                                    byte[] response = null;
                                    response = GetResponse(dataPacket);

                                    if (response != null)
                                    {
                                        if (NoAckForIP == null || NoAckForIP != remote.Address)
                                        {
                                            Utils.LogToFile(3, "[INFO]", $"Ack IP:{remote.Address}, Port:{remote.Port}");
                                            Utils.LogToFile(3, "[INFO]", "Sending Ack To The Client");
                                            Client.Send(response,response.Length,remote);
                                            Utils.LogToFile(3, "[INFO]", $"Response Message:{BitConverter.ToString(response)}");
                                            Utils.LogToFile(3, "[INFO]", $"Original Message:{BitConverter.ToString(inputData)}");
                                        }
                                        else
                                        {
                                            Utils.LogToFile(3, "[INFO]", $"No Ack For IP:{remote.Address}");
                                        }
                                    }                           

                                }
                                catch (Exception ex)
                                {
                                    Utils.LogToFile(1, $"[EXCEPTION]",$"ACK Exception In RunBulk(): {ex.Message.ToString()} Stacktrace:{ex.StackTrace.ToString()}");
                                }
                                udpStateListIndex++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Utils.LogToFile(1, $"[EXCEPTION]", $"Exception In RunBulk(): {ex.Message.ToString()} Stacktrace:{ex.StackTrace.ToString()}");
                        }
                    }
                  
                }
                catch (Exception ex)
                {
                    Utils.LogToFile(1, $"[EXCEPTION]", $"Exception In RunBulk(): {ex.Message.ToString()} Stacktrace:{ex.StackTrace.ToString()}");
                }

            }
        }

        byte[] GetResponse(UdpDataPacket udpDataPacket)
        {
            Utils.LogToFile(3, "[INFO]", "Calling GetResponse()");
            var bytes = new List<byte>();

            const short packetLength = 2 /* Id */+ 1 /* Type */ + 1 /* Avl packet id */+ 1 /* num of accepted elements */;
            bytes.AddRange(BitConverter.GetBytes(BytesSwapper.Swap(packetLength)));
            bytes.AddRange(BitConverter.GetBytes(BytesSwapper.Swap(udpDataPacket.PacketId)));
            bytes.Add(udpDataPacket.PacketType);
            bytes.Add(udpDataPacket.AvlPacketId);
            bytes.Add((byte)udpDataPacket.AvlData.DataCount);

            return bytes.ToArray();
        }

        public void SetDbSaver(DatabaseSaver databaseSaver)
        {
            Utils.LogToFile(3, "[INFO]", "Calling SetDbSaver()");
            _dbSaver = databaseSaver;
        }
        private static UdpDataPacket RawDeserialize(byte[] rawData)
        {
            Utils.LogToFile(6, "[INFO]", "Calling RawDeserialize()");
            var reader = new ReverseBinaryReader(new MemoryStream(rawData));
            var avlData = new DataDecoder(reader).DecodeUdpData();
            return avlData;
        }

        public string ParseToSQL(string sourceIP, string imei, AvlData data)
        {
            Utils.LogToFile(3, "[INFO]", "Calling ParseToSQL()");
            string result = string.Empty;

            long eventId = -1;
            long eventValue = -1;

            eventId = data.IoElement.EventId;

            if (eventId > 0)
            {
                eventValue = (long)data.IoElement.Properties.Where(x => x.Id == eventId).FirstOrDefault().Value;


            }

            int ilng = int.Parse(data.GpsElement.X.ToString(), System.Globalization.NumberStyles.Float);
          
            int ilat = int.Parse(data.GpsElement.Y.ToString(), System.Globalization.NumberStyles.Float);
       
            IoProperty? lastKnownNode = data.IoElement.Properties.Where(property => property.Id == 69).FirstOrDefault();

            int? eventValueForLastKnown = (lastKnownNode.HasValue ? (int?)lastKnownNode.Value.Value : null);
            int lastknown = -1;

            if (eventValueForLastKnown.HasValue && eventValueForLastKnown == 1)
            {
                lastknown = 0;
            }
            else
            {
                lastknown = 1;
            }

            string IoEventList = ConfigurationManager.AppSettings["IOEventList"];
            string RequiredIoEventList = "";
            string[] ParsedIoEventList = IoEventList.Split(',');

            for (int i = 0; i < ParsedIoEventList.Length; i++)
            {
                string[] eventInfo = ParsedIoEventList[i].Split(':');
                long? RequiredEventValue = data.IoElement.Properties.FirstOrDefault(property => property.Id == Int32.Parse(eventInfo[0])).Value;

                if (RequiredEventValue != null)
                {
                    RequiredIoEventList = RequiredIoEventList + "" + eventInfo[0] + "" + ":" + "" + RequiredEventValue + "";
                }
                else
                {
                    RequiredIoEventList = RequiredIoEventList + "" + eventInfo[0] + "" + ":" + "" + eventInfo[1] + "";
                }

                if (i != ParsedIoEventList.Length - 1)
                {
                    RequiredIoEventList = RequiredIoEventList + ",";
                }
            }

            result = String.Format("exec hstGpsServiceTeltonika @Iimei='{0}', @i_lat={1}, @i_lon={2} " +
                " ,@speed={3},@course={4},@clientTime='{5}' " +
                " ,@quality={6},@source='{7}',@IpAddress='{8}' " +
                " ,@EventId='{9}',@EventValue='{10}', @listener={11}, @LastKnown={12},@Accumulators='{13}' "
                , imei, ilat, ilng
                , data.GpsElement.Speed, data.GpsElement.Angle, data.DateTime
                , data.GpsElement.Satellites, "Teltonika", sourceIP
                , eventId, eventValue, Utils.GetListenerId(), lastknown, RequiredIoEventList
                );

            Utils.LogToFile(3, "[INFO]", $"RawDeserialize:- Imei:{imei}, i_lat:{ilat}, i_lon: {ilng}, " +
                                        $"speed: {data.GpsElement.Speed}, course: {data.GpsElement.Angle}, clientTime:" +
                                        $"{data.DateTime}, quality: {data.GpsElement.Satellites}, Source: Teltonika, IpAddress: {sourceIP}, EventId: {eventId}, " +
                                        $"EventValue: {eventValue}, Listener: {Utils.GetListenerId()}, LastKnown: {lastknown}, Accumulators: {RequiredIoEventList}");

            return result;
        }

    }
}

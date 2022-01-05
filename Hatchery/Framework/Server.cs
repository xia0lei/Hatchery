using System;
using System.IO;
using System.Threading;
using Google.Protobuf;
using Hatchery.Framework.MessageQueue;
using Hatchery.Framework.Network;
using Hatchery.Framework.ProtoSchema;
using Hatchery.Framework.Service;
using Hatchery.Framework.Timer;
using Hatchery.Framework.Utility;
using Newtonsoft.Json.Linq;

namespace Hatchery.Framework
{
    public delegate void BootServices();

    public class Server
    {
        JObject m_bootConfig;
        private int m_workerNum = 8;
        private string m_clusterServerIp;
        private int m_clusterServerPort = 0;
        private string m_gateIp;
        private int m_gatePort = 0;
        private TCPServer m_clusterTCPServer;
        private TCPClient m_clusterTCPClient;
        private TCPServer m_tcpGate;
        private TCPObjectContainer m_tcpObjectContainer;
        private GlobalMQ m_globalMQ;
        private ServiceSlots m_serviceSlots;
        private NetworkPacketQueue m_netpackQueue;
        private SSTimer m_timer;

        public void Run(string bootConf, BootServices customBoot)
        {
            HatcheryUtility.InitBootConf(bootConf);
            InitConfig(bootConf);
            Boot(customBoot);
            Loop();
        }

        private void InitConfig(string bootConf)
        {
            string bootConfigText = ConfigHelper.LoadFromFile(bootConf);
            m_bootConfig = JObject.Parse(bootConfigText);

            if (m_bootConfig.ContainsKey("ClusterConfig"))
            {
                string clusterNamePath = m_bootConfig["ClusterConfig"].ToString();
                string clusterNameText = ConfigHelper.LoadFromFile(clusterNamePath);
                JObject clusterConfig = JObject.Parse(clusterNameText);

                string clusterName = m_bootConfig["ClusterName"].ToString();
                string ipEndPoint = clusterConfig[clusterName].ToString();

                string[] ipResult = ipEndPoint.Split(':');
                m_clusterServerIp = ipResult[0];
                m_clusterServerPort = Int32.Parse(ipResult[1]);
            }

            if (m_bootConfig.ContainsKey("Gateway"))
            {
                string gatewayEndpoint = m_bootConfig["Gateway"]["Host"].ToString();
                string[] ipResult = gatewayEndpoint.Split(":");
                m_gateIp = ipResult[0];
                m_gatePort = Int32.Parse(ipResult[1]);
            }

            if (m_bootConfig.ContainsKey("ThreadNum"))
            {
                int threadNum = (int)m_bootConfig["ThreadNum"];
                if (threadNum > 0)
                    m_workerNum = threadNum;
            }
        }

        private void Boot(BootServices customBoot)
        {
            m_globalMQ = GlobalMQ.GetInstance();
            m_serviceSlots = ServiceSlots.GetInstance();
            m_netpackQueue = NetworkPacketQueue.GetInstance();
            m_timer = SSTimer.GetInstance();

            Logger_Init loggerInit = new Logger_Init();
            if (m_bootConfig.ContainsKey("Logger"))
            {
                if (Directory.Exists(m_bootConfig["Logger"].ToString()))
                {
                    loggerInit.LoggerPath = System.IO.Path.GetFullPath(m_bootConfig["Logger"].ToString());
                }
                else
                {
                    DirectoryInfo di = Directory.CreateDirectory(m_bootConfig["Logger"].ToString());
                    if (di.Exists)
                    {
                        loggerInit.LoggerPath = System.IO.Path.GetFullPath(m_bootConfig["Logger"].ToString());
                    }
                    else
                    {
                        loggerInit.LoggerPath = "../";
                    }
                }
            }
            else
            {
                loggerInit.LoggerPath = "../";
            }
            HatcheryUtility.NewService("Hatchery.Framework.Service.Logger.LoggerService", "logger", loggerInit.ToByteArray());

            m_tcpObjectContainer = new TCPObjectContainer();
            if (m_bootConfig.ContainsKey("ClusterConfig"))
            {
                InitCluster();
            }

            if (m_bootConfig.ContainsKey("Gateway"))
            {
                InitGateway();
            }

            customBoot();

            LoggerHelper.Info(0, "Start Hatchery Server...");

            for(int i = 0; i < m_workerNum; i++)
            {
                Thread thread = new Thread(new ThreadStart(ThreadWorker));
                thread.Start();
            }
            Thread timerThread = new Thread(new ThreadStart(ThreadTimer));
            timerThread.Start();
        }

        private void ThreadWorker()
        {
            AutoResetEvent autoResetEvent = new AutoResetEvent(false);
            while (true)
            {
                int serviceId = m_globalMQ.Pop();
                if (serviceId == 0)
                {
                    autoResetEvent.WaitOne(1);
                }
                else
                {
                    ServiceContext service = m_serviceSlots.Get(serviceId);
                    Message msg = service.Pop();
                    if (msg != null)
                    {
                        service.Callback(msg);
                        m_globalMQ.Push(service.GetId());
                    }
                }
            }
        }

        private void ThreadTimer()
        {
            while (true)
            {
                m_timer.Loop();
                Thread.Sleep(1);
            }
        }

        private void InitGateway()
        {
            string gatewayClass = m_bootConfig["Gateway"]["Class"].ToString();
            string gatewayName = m_bootConfig["Gateway"]["Name"].ToString();

            m_tcpGate = new TCPServer();
            m_tcpObjectContainer.Add(m_tcpGate);

            Gateway_Init gateInit = new Gateway_Init();
            gateInit.TcpServerId = m_tcpGate.GetObjectId();

            LoggerHelper.Info(0, String.Format("InitGateWay {0}", gateInit.ToString()));
            int gatewayId = HatcheryUtility.NewService(gatewayClass, gatewayName, gateInit.ToByteArray());
            m_tcpGate.Start(m_gateIp, m_gatePort, 30, gatewayId, OnSessionError, OnReadPacketComplete, OnAcceptComplete);
        }

        private void InitCluster()
        {
            m_clusterTCPServer = new TCPServer();
            m_tcpObjectContainer.Add(m_clusterTCPServer);

            m_clusterTCPClient = new TCPClient();
            m_tcpObjectContainer.Add(m_clusterTCPClient);

            ClusterServer_Init clusterServerInit = new ClusterServer_Init();
            clusterServerInit.TcpServerId = m_clusterTCPServer.GetObjectId();
            int clusterServerId = HatcheryUtility.NewService("Hatchery.Framework.Service.ClusterServer.ClusterServer", "clusterserver", clusterServerInit.ToByteArray());

            ClusterClient_Init clusterClient_Init = new ClusterClient_Init();
            clusterClient_Init.TcpClientId = m_clusterTCPClient.GetObjectId();
            clusterClient_Init.ClusterConfig = m_bootConfig["ClusterConfig"].ToString();
            int clusterClientId = HatcheryUtility.NewService("Hatchery.Framework.Service.ClusterClient.ClusterClient", "clusterClient", clusterClient_Init.ToByteArray());

            m_clusterTCPServer.Start(m_clusterServerIp, m_clusterServerPort, 30, clusterServerId, OnSessionError, OnReadPacketComplete, OnAcceptComplete);
            m_clusterTCPClient.Start(clusterClientId, OnSessionError, OnReadPacketComplete, OnConnectedComplete);
        }

        private void OnReadPacketComplete(int opaque, long sessionId, byte[] buffer, int packetSize)
        {
            SocketData data = new SocketData();
            data.Connection = sessionId;
            data.Buffer = Convert.ToBase64String(buffer);

            Message msg = new Message();
            msg.Source = 0;
            msg.Destination = opaque;
            msg.Method = "SocketData";
            msg.Data = data.ToByteArray();
            msg.RPCSession = 0;
            msg.Type = MessageType.Socket;

            ServiceContext service = ServiceSlots.GetInstance().Get(opaque);
            service.Push(msg);
        }

        private void OnSessionError(int opaque, long sessionId, string remoteEndPoint, int errorCode, string errorText)
        {
            SocketError sprotoSocketError = new SocketError();
            sprotoSocketError.ErrorCode = errorCode;
            sprotoSocketError.ErrorText = errorText;
            sprotoSocketError.Connection = sessionId;
            sprotoSocketError.RemoteEndPoint = remoteEndPoint;

            Message msg = new Message();
            msg.Source = 0;
            msg.Destination = opaque;
            msg.Method = "SocketError";
            msg.Data = sprotoSocketError.ToByteArray(); 
            msg.RPCSession = 0;
            msg.Type = MessageType.Socket;

            ServiceContext service = ServiceSlots.GetInstance().Get(opaque);
            service.Push(msg);
        }


        private void OnAcceptComplete(int opaque, long sessionId, string ip, int port)
        {
            SocketAccept accept = new SocketAccept();
            accept.Connection = sessionId;
            accept.Ip = ip;
            accept.Port = port;

            Message msg = new Message();
            msg.Source = 0;
            msg.Destination = opaque;
            msg.Method = "SocketAccept";
            msg.Data = accept.ToByteArray();
            msg.RPCSession = 0;
            msg.Type = MessageType.Socket;

            ServiceContext service = ServiceSlots.GetInstance().Get(opaque);
            service.Push(msg);
        }

        private void OnConnectedComplete(int opaque, long sessionId, string ip, int port)
        {
            ClusterClientSocketConnected connected = new ClusterClientSocketConnected();
            connected.Connection = sessionId;
            connected.Ip = ip;
            connected.Port = port;

            Message msg = new Message();
            msg.Source = 0;
            msg.Destination = opaque;
            msg.Method = "SocketConnected";
            msg.Data = connected.ToByteArray();
            msg.RPCSession = 0;
            msg.Type = MessageType.Socket;

            ServiceContext service = ServiceSlots.GetInstance().Get(opaque);
            service.Push(msg);
        }

        private void ProcessOutbound()
        {
            while (true)
            {
                SocketMessage socketMessage = m_netpackQueue.Pop();
                if (socketMessage == null)
                    break;

                switch (socketMessage.Type)
                {
                    case SocketMessageType.Connect:
                        {
                            ConnectMessage conn = socketMessage as ConnectMessage;
                            TCPClient tcpClient = (TCPClient)m_tcpObjectContainer.Get(conn.TcpObjectId);
                            tcpClient.Connect(conn.IP, conn.Port);
                        }
                        break;
                    case SocketMessageType.Disconnect:
                        {
                            DisconnectMessage conn = socketMessage as DisconnectMessage;
                            TCPObject tcpObject = m_tcpObjectContainer.Get(conn.TcpObjectId);
                            tcpObject.Disconnect(conn.ConnectionId);
                        }
                        break;
                    case SocketMessageType.DATA:
                        {
                            NetworkPacket netpack = socketMessage as NetworkPacket;
                            TCPObject tcpObject = m_tcpObjectContainer.Get(netpack.TcpObjectId);
                            Session session = tcpObject.GetSessionBy(netpack.ConnectionId);
                            if (session != null)
                            {
                                for (int i = 0; i < netpack.Buffers.Count; i++)
                                {
                                    session.Write(netpack.Buffers[i]);
                                }
                            }
                            else
                            {
                                LoggerHelper.Info(0, string.Format("Opaque:{0} ConnectionId:{1} ErrorText:{2}", tcpObject.GetOpaque(), netpack.ConnectionId, "Connection disconnected"));
                            }
                        }
                        break;
                    default: break;
                }
            }
        }

        private void Loop()
        {
            bool isInitCluster = m_bootConfig.ContainsKey("ClusterConfig");
            bool isInitGateway = m_bootConfig.ContainsKey("Gateway");
            while (true)
            {
                if (isInitCluster)
                {
                    m_clusterTCPServer.Loop();
                    m_clusterTCPClient.Loop();
                }

                if (isInitGateway)
                {
                    m_tcpGate.Loop();
                }

                ProcessOutbound();

                Thread.Sleep(1);
            }
        }
    }

}

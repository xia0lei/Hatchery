using System;
using System.Collections.Generic;
using System.Diagnostics;
using Google.Protobuf;
using Hatchery.Framework.MessageQueue;
using Hatchery.Framework.ProtoSchema;
using Hatchery.Framework.Utility;
using Newtonsoft.Json.Linq;

namespace Hatchery.Framework.Service.ClusterClient
{
    class WaitForSendRequest
    {
        public int Source { get; set; }
        public int Session { get; set; }
        public string Method { get; set; }
        public ClusterClientRequest Request { get; set; }
    }

    class WaitForResponseRequest
    {
        public int Source { get; set; }
        public int Session { get; set; }
    }

    class ClusterClient : ServiceContext
    {
        private Dictionary<string, long> m_node2conn = new Dictionary<string, long>();
        private Dictionary<string, Queue<WaitForSendRequest>> m_waitForSendRequests = new Dictionary<string, Queue<WaitForSendRequest>>();
        private Dictionary<int, RPCResponseContext> m_remoteResponseCallbacks = new Dictionary<int, RPCResponseContext>();
        private Dictionary<long, Dictionary<int, WaitForResponseRequest>> m_conn2sessions = new Dictionary<long, Dictionary<int, WaitForResponseRequest>>();
        private int m_totalRemoteSession = 0;

        Dictionary<string, Method> m_socketMethods = new Dictionary<string, Method>();
        private int m_tcpObjectId = 0;

        private SkynetPacketManager m_skynetPacketManager = new SkynetPacketManager();

        private JObject m_clusterConfig = new JObject();

        protected override void Init(byte[] param)
        {
            base.Init();

            ClusterClient_Init init = ProtoSchema.ClusterClient_Init.Parser.ParseFrom(param);
            SetTCPObjectId((int)init.TcpClientId);
            ParseClusterConfig(init.ClusterConfig);

            RegisterSocketMethods("SocketConnected", SocketConnected);
            RegisterSocketMethods("SocketError", SocketError);
            RegisterSocketMethods("SocketData", SocketData);

            RegisterServiceMethods("Request", Request);
        }

        private void RegisterSocketMethods(string methodName, Method method)
        {
            m_socketMethods.Add(methodName, method);
        }

        private void Request(int source, int session, string method, byte[] param)
        {
            var request = ProtoSchema.ClusterClientRequest.Parser.ParseFrom(param);
            string remoteNode = request.RemoteNode;
            string ipEndPoint = "";
            if (m_clusterConfig.ContainsKey(remoteNode))
            {
                ipEndPoint = m_clusterConfig[remoteNode].ToString();
            }
            else
            {
                DoError(source, session, RPCError.UnknowRemoteNode, "Unknow Remote Node");
                return;
            }

            long connectionId = 0;

            bool isExist = m_node2conn.TryGetValue(ipEndPoint, out connectionId);
            if (isExist)
            {
                RemoteRequest(source, method, request, connectionId, session);
            }
            else
            {
                CacheRequest(source, session, method, request, remoteNode);
            }
        }

        private void CacheRequest(int source, int session, string method, ClusterClientRequest request, string remoteNode)
        {
            string ipEndpoint = m_clusterConfig[remoteNode].ToString();
            Queue<WaitForSendRequest> waittingQueue = null;
            bool isExist = m_waitForSendRequests.TryGetValue(ipEndpoint, out waittingQueue);
            if (!isExist)
            {
                waittingQueue = new Queue<WaitForSendRequest>();
                m_waitForSendRequests.Add(ipEndpoint, waittingQueue);
            }

            if (waittingQueue.Count <= 0)
            {
                string[] ipResult = ipEndpoint.Split(':');
                string remoteIp = ipResult[0];
                int remotePort = Int32.Parse(ipResult[1]);

                ConnectMessage connectMessage = new ConnectMessage();
                connectMessage.IP = remoteIp;
                connectMessage.Port = remotePort;
                connectMessage.TcpObjectId = m_tcpObjectId;
                connectMessage.Type = SocketMessageType.Connect;

                NetworkPacketQueue.GetInstance().Push(connectMessage);
            }

            WaitForSendRequest waitRequest = new WaitForSendRequest();
            waitRequest.Source = source;
            waitRequest.Session = session;
            waitRequest.Method = method;
            waitRequest.Request = request;

            waittingQueue.Enqueue(waitRequest);
        }

        private void SetTCPObjectId(int tcpObjectId)
        {
            m_tcpObjectId = tcpObjectId;
        }

        private void ParseClusterConfig(string clusterPath)
        {
            string clusterConfigText = ConfigHelper.LoadFromFile(clusterPath);
            m_clusterConfig = JObject.Parse(clusterConfigText);
        }

        protected override void OnSocketCommand(Message msg)
        {
            base.OnSocketCommand(msg);

            Method method = null;
            bool isExist = m_socketMethods.TryGetValue(msg.Method, out method);
            if (isExist)
            {
                method(msg.Source, msg.RPCSession, msg.Method, msg.Data);
            }
            else
            {
                LoggerHelper.Info(m_serviceAddress, string.Format("ClusterClient unknow socket method:{0}", msg.Method));
            }
        }

        private void RemoteRequest(int source, string method, ClusterClientRequest request, long connectionId, int session)
        {
            RPCParam rpcParam = new RPCParam();
            rpcParam.Method = request.Method;
            rpcParam.Param = request.Param;

            if (m_totalRemoteSession >= Int32.MaxValue)
            {
                m_totalRemoteSession = 0;
            }
            int remoteSession = ++m_totalRemoteSession;
            List<byte[]> buffers = m_skynetPacketManager.PackSkynetRequest(request.RemoteService, remoteSession, (int)CommonMsgID.COMMON_RPCPARAM, rpcParam.ToByteArray()) ;

            SSContext rpcContext = new SSContext();
            rpcContext.LongDict["ConnectionId"] = connectionId;
            rpcContext.IntegerDict["RemoteSession"] = remoteSession;
            rpcContext.IntegerDict["SourceSession"] = session;
            rpcContext.IntegerDict["Source"] = source;
            rpcContext.StringDict["Method"] = method;

            RPCResponseContext rpcResponseCallback = new RPCResponseContext();
            rpcResponseCallback.Callback = RemoteResponseCallback;
            rpcResponseCallback.Context = rpcContext;
            m_remoteResponseCallbacks.Add(remoteSession, rpcResponseCallback);

            Dictionary<int, WaitForResponseRequest> waitResponseDict = null;
            bool isExist = m_conn2sessions.TryGetValue(connectionId, out waitResponseDict);
            if (!isExist)
            {
                waitResponseDict = new Dictionary<int, WaitForResponseRequest>();
                m_conn2sessions.Add(connectionId, waitResponseDict);
            }

            WaitForResponseRequest waitForResponseRequest = new WaitForResponseRequest();
            waitForResponseRequest.Session = session;
            waitForResponseRequest.Source = source;
            waitResponseDict.Add(remoteSession, waitForResponseRequest);

            NetworkPacket networkPacket = new NetworkPacket();
            networkPacket.ConnectionId = connectionId;
            networkPacket.TcpObjectId = m_tcpObjectId;
            networkPacket.Buffers = buffers;
            networkPacket.Type = SocketMessageType.DATA;

            NetworkPacketQueue.GetInstance().Push(networkPacket);
        }

        private void SocketConnected(int source, int session, string method, byte[] param)
        {
            ClusterClientSocketConnected socketConnected = ProtoSchema.ClusterClientSocketConnected.Parser.ParseFrom(param);
            string ipEndpoint = string.Format("{0}:{1}", socketConnected.Ip, socketConnected.Port);

            long tempConnection = 0;
            bool isExist = m_node2conn.TryGetValue(ipEndpoint, out tempConnection);
            if (isExist)
            {
                m_node2conn.Remove(ipEndpoint);
            }
            m_node2conn.Add(ipEndpoint, socketConnected.Connection);

            Queue<WaitForSendRequest> waitQueue = null;
            isExist = m_waitForSendRequests.TryGetValue(ipEndpoint, out waitQueue);
            if (isExist)
            {
                int count = waitQueue.Count;
                for (int i = 0; i < count; i++)
                {
                    WaitForSendRequest request = waitQueue.Dequeue();
                    RemoteRequest(request.Source, request.Method, request.Request, socketConnected.Connection, request.Session);
                }
                m_waitForSendRequests.Remove(ipEndpoint);
            }
        }

        private void SocketData(int source, int session, string method, byte[] param)
        {
            SocketData socketData = ProtoSchema.SocketData.Parser.ParseFrom(param);
            long connectionId = socketData.Connection;
            byte[] tempParam = Convert.FromBase64String(socketData.Buffer);

            SkynetClusterResponse response = m_skynetPacketManager.UnpackSkynetResponse(tempParam);
            if (response == null)
            {
                return;
            }

            byte[] targetParam = null;
            if (response.ErrorCode == RPCError.OK)
            {
                RPCParam sprotoResponse = ProtoSchema.RPCParam.Parser.ParseFrom(response.Data);
                targetParam = Convert.FromBase64String(sprotoResponse.Param);
            }

            int remoteSession = response.Session;
            ProcessRemoteResponse(remoteSession, targetParam, response.ErrorCode);
        }

        private void RemoteResponseCallback(SSContext context, string method, byte[] param, RPCError error)
        {
            long connectionId = context.LongDict["ConnectionId"];
            int remoteSession = context.IntegerDict["RemoteSession"];
            int sourceSession = context.IntegerDict["SourceSession"];
            int source = context.IntegerDict["Source"];
            string sourceMethod = context.StringDict["Method"];

            if (error == RPCError.OK)
            {
                DoResponse(source, sourceMethod, param, sourceSession);
            }
            else
            {
                DoError(source, sourceSession, error, "RemoteCall Error");
            }

            Dictionary<int, WaitForResponseRequest> waitForResponseDict = null;
            bool isExist = m_conn2sessions.TryGetValue(connectionId, out waitForResponseDict);
            if (isExist)
            {
                waitForResponseDict.Remove(remoteSession);
            }
        }

        private void SocketError(int source, int session, string method, byte[] param)
        {
            SocketError error = ProtoSchema.SocketError.Parser.ParseFrom(param);
            long connectionId = 0;
            bool canFind = m_node2conn.TryGetValue(error.RemoteEndPoint, out connectionId);

            // if connection already exist, that means queue of waitForRequest is empty, because 
            // it will send and clear after connect success
            if (canFind)
            {
                Debug.Assert(connectionId == error.Connection);

                Dictionary<int, WaitForResponseRequest> waitForResponseRequests = null;
                bool isExist = m_conn2sessions.TryGetValue(error.Connection, out waitForResponseRequests);
                if (isExist)
                {
                    Queue<int> tempRemoteSessions = new Queue<int>();
                    foreach (var pair in waitForResponseRequests)
                    {
                        tempRemoteSessions.Enqueue(pair.Key);
                    }

                    int count = tempRemoteSessions.Count;
                    for (int i = 0; i < count; i++)
                    {
                        int remoteSession = tempRemoteSessions.Dequeue();
                        ProcessRemoteResponse(remoteSession, null, RPCError.SocketDisconnected);
                    }
                    m_conn2sessions.Remove(error.Connection);
                    m_node2conn.Remove(error.RemoteEndPoint);
                }
            }
            else
            {
                string ipEndpoint = error.RemoteEndPoint;
                Queue<WaitForSendRequest> waitQueue = null;
                bool isExist = m_waitForSendRequests.TryGetValue(ipEndpoint, out waitQueue);
                if (isExist)
                {
                    int count = waitQueue.Count;
                    for (int i = 0; i < count; i++)
                    {
                        WaitForSendRequest req = waitQueue.Dequeue();
                        DoError(req.Source, req.Session, RPCError.SocketDisconnected, string.Format("RemoteCall {0} failure", req.Method));
                    }

                    m_waitForSendRequests.Remove(ipEndpoint);
                }
            }
        }

        private void ProcessRemoteResponse(int remoteSession, byte[] param, RPCError errorCode)
        {
            RPCResponseContext responseCallback = null;
            bool isExist = m_remoteResponseCallbacks.TryGetValue(remoteSession, out responseCallback);
            if (isExist)
            {
                responseCallback.Callback(responseCallback.Context, "RemoteResponseCallback", param, errorCode);
                m_remoteResponseCallbacks.Remove(remoteSession);
            }
            else
            {
                LoggerHelper.Info(m_serviceAddress, string.Format("ClusterServer SocketData unknow remoteSession:{0}", remoteSession));
            }
        }
    }
  
}

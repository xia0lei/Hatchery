using System;
using System.Collections.Generic;
using Hatchery.Framework.ProtoSchema;
using Hatchery.Framework.Utility;
using Hatchery.Framework.MessageQueue;
using Hatchery.Framework.Service;
using Google.Protobuf;
using System.Text;

namespace Hatchery.Framework.Service.ClusterServer
{
    public class ClusterServer : ServiceContext
    {
        private int m_tcpObjectId = 0;
        private SkynetPacketManager m_skynetPacketManager = new SkynetPacketManager();
        Dictionary<string, Method> m_socketMethods = new Dictionary<string, Method>();

        public ClusterServer()
        {
        }

        protected override void Init(byte[] param)
        {
            base.Init();
            ClusterServer_Init init = ClusterServer_Init.Parser.ParseFrom(param);
            SetTCPObject((int)init.TcpServerId);

            RegisterSocketMethods("SocketAccept", SocketAccept);
            RegisterSocketMethods("SocketError", SocketError);
            RegisterSocketMethods("SocketData", SocketData);
        }

         private void RegisterSocketMethods(string methodName, Method method)
        {
            m_socketMethods.Add(methodName, method);
        }

        private void SocketData(int source, int session, string method, byte[] param)
        {
            SocketData socketData = ProtoSchema.SocketData.Parser.ParseFrom(param);
            long connectionId = socketData.Connection;
            byte[] tempParam = Convert.FromBase64String(socketData.Buffer);

            SkynetClusterRequest req = m_skynetPacketManager.UnpackSkynetRequest(tempParam);
            if(req == null)
            {
                return;
            }

            RPCParam rpc = ProtoSchema.RPCParam.Parser.ParseFrom(req.Data);
            LoggerHelper.Info(m_serviceAddress, String.Format("Cluster Server {0}", rpc.Method));
            byte[] targetParam = Convert.FromBase64String(rpc.Param);

            if (req.Session > 0)
            {
                SSContext context = new SSContext();
                context.IntegerDict["RemoteSession"] = req.Session;
                context.LongDict["ConnectionId"] = connectionId;
                Call(req.ServiceName, rpc.Method, targetParam, context, TransferCallback);
            }
            else
            {
                Send(req.ServiceName, rpc.Method, targetParam);
            }
        }

        private void TransferCallback(SSContext context, string method, byte[] param, RPCError error)
        {
            if(error == RPCError.OK)
            {
                RPCParam rpcParam = new RPCParam();
                rpcParam.Method = method;
                // if(method.Equals("OnCreateBattleField"))
                // {
                //     var response = CreateBattleFieldResp.Parser.ParseFrom(param);
				// 	LoggerHelper.Info(m_serviceAddress, String.Format("TransferCallback {0}, {1}, {2}, {3}", method, response.Ret, response.Seed, response.BattleId));
                // }
                rpcParam.Param = Convert.ToBase64String(param);

                int remoteSession = context.IntegerDict["RemoteSession"];
                long connectionId = context.LongDict["ConnectionId"];

                List<byte[]> bufferList = m_skynetPacketManager.PackSkynetResponse(remoteSession, (int)CommonMsgID.COMMON_RPCPARAM, rpcParam.ToByteArray());

                NetworkPacket rpcMessage = new NetworkPacket();
                rpcMessage.Type = SocketMessageType.DATA;
                rpcMessage.TcpObjectId = m_tcpObjectId;
                rpcMessage.Buffers = bufferList;
                rpcMessage.ConnectionId = connectionId;

                NetworkPacketQueue.GetInstance().Push(rpcMessage);
            }
            else
            {
                int remoteSession = context.IntegerDict["RemoteSession"];
                long connectionId = context.LongDict["ConnectionId"];

                List<byte[]> bufferList = m_skynetPacketManager.PackErrorResponse(remoteSession, Encoding.ASCII.GetString(param));
                NetworkPacket rpcMessage = new NetworkPacket();
                rpcMessage.Type = SocketMessageType.DATA;
                rpcMessage.TcpObjectId = m_tcpObjectId;
                rpcMessage.Buffers = bufferList;
                rpcMessage.ConnectionId = connectionId;

                NetworkPacketQueue.GetInstance().Push(rpcMessage);
                LoggerHelper.Info(m_serviceAddress,
                    string.Format("Service:ClusterServer Method:TransferCallback errorCode:{0} errorText:{1}", (int)error, Encoding.ASCII.GetString(param)));
            }
        }

        private void SocketError(int source, int session, string method, byte[] param)
        {
            SocketError error = ProtoSchema.SocketError.Parser.ParseFrom(param);
            LoggerHelper.Info(m_serviceAddress,
                string.Format("ClusterServer socket error connection:{0} errorCode:{1} errorText:{2}", error.Connection, error.ErrorCode, error.ErrorText));
        }

        private void SocketAccept(int source, int session, string method, byte[] param)
        {
            SocketAccept accept = ProtoSchema.SocketAccept.Parser.ParseFrom(param);
            LoggerHelper.Info(m_serviceAddress,
                 string.Format("ClusterServer accept new connection ip = {0}, port = {1}, connection = {2}", accept.Ip, accept.Port, accept.Connection));
        }

        private void SetTCPObject(int tcpServerId)
        {
            m_tcpObjectId = tcpServerId;
        }

        protected override void OnSocketCommand(Message msg)
        {
            LoggerHelper.Info(m_serviceAddress, msg.ToString());
            base.OnSocketCommand(msg);

            Method method = null;
            bool isExist = m_socketMethods.TryGetValue(msg.Method, out method);
            if (isExist)
            {
                method(msg.Source, msg.RPCSession, msg.Method, msg.Data);
            }
            else
            {
                LoggerHelper.Info(m_serviceAddress, string.Format("Unknow socket command {0}", msg.Method));
            }
        }
    }
}

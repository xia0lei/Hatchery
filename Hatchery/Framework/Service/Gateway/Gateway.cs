using System;
using System.Collections.Generic;
using Hatchery.Framework.ProtoSchema;
using Hatchery.Framework.Utility;

namespace Hatchery.Framework.Service.Gateway
{
    public class Gateway: ServiceContext
    {
        private Dictionary<string, Method> m_socketMethods = new Dictionary<string, Method>();
        private int m_tcpObjectId = 0;

        protected override void Init(byte[] param)
        {
            base.Init();

            Gateway_Init gatewayInit = ProtoSchema.Gateway_Init.Parser.ParseFrom(param);
            SetTCPObjectId((int)gatewayInit.TcpServerId);

            RegisterSocketMethods("SocketAccept", SocketAccept);
            RegisterSocketMethods("SocketData", SocketData);
            RegisterSocketMethods("SocketError", SocketError);
        }

        private void SetTCPObjectId(int tcpObjectId)
        {
            m_tcpObjectId = tcpObjectId;
        }

        protected int GetTcpObjectId()
        {
            return m_tcpObjectId;
        }

        protected override void OnSocketCommand(Message msg)
        {
            Method method = null;
            bool isExist = m_socketMethods.TryGetValue(msg.Method, out method);
            if (isExist)
            {
                method(msg.Source, msg.RPCSession, msg.Method, msg.Data);
            }
            else
            {
                LoggerHelper.Info(m_serviceAddress, string.Format("unknow method {0}", msg.Method));
            }
        }

        protected virtual void SocketAccept(int source, int session, string method, byte[] param)
        {

        }

        protected virtual void SocketError(int source, int session, string method, byte[] param)
        {

        }

        protected virtual void SocketData(int source, int session, string method, byte[] param)
        {

        }

        private void RegisterSocketMethods(string methodName, Method method)
        {
            m_socketMethods.Add(methodName, method);
        }
    }
}

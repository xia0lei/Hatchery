using System;
using System.Collections.Generic;
using System.Text;
using Hatchery.Framework.MessageQueue;
using Hatchery.Framework.ProtoSchema;
using Hatchery.Framework.Utility;

namespace Hatchery.Test.Gateway
{
    public class GatewayCase : Hatchery.Framework.Service.Gateway.Gateway
    {
        protected override void Init()
        {
            base.Init();
        }

        protected override void SocketAccept(int source, int session, string method, byte[] param)
        {
            LoggerHelper.Info(m_serviceAddress, "GatewayCase.SocketAccept");
        }

        protected override void SocketError(int source, int session, string method, byte[] param)
        {
            LoggerHelper.Info(m_serviceAddress, "GatewayCase.SocketError");
        }

        protected override void SocketData(int source, int session, string method, byte[] param)
        {
            SocketData data = Hatchery.Framework.ProtoSchema.SocketData.Parser.ParseFrom(param);
            LoggerHelper.Info(m_serviceAddress, String.Format("GatewayCase.SocketData:{0},{1},{2}", data.Connection, data.Buffer, Encoding.ASCII.GetString(Convert.FromBase64String(data.Buffer))));
            NetworkPacket message = new NetworkPacket();
            message.Type = Hatchery.Framework.MessageQueue.SocketMessageType.DATA;
            message.TcpObjectId = this.GetTcpObjectId();
            message.ConnectionId = data.Connection;

            List<byte[]> buffList = new List<byte[]>();
            buffList.Add(Convert.FromBase64String(data.Buffer));
            message.Buffers = buffList;

            NetworkPacketQueue.GetInstance().Push(message);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.Protobuf;
using Hatchery.Framework.MessageQueue;
using Hatchery.Framework.Service;
using Hatchery.Framework.Utility;

namespace Hatchery.Battle.Handler
{
	public delegate void HandleMethod(Int64 connectionId, IMessage message, MsgID retMsgID, RPCCallback cb);
	public class Handler
	{
		private Dictionary<MsgID, HandleMethod> m_handleMethods = new Dictionary<MsgID, HandleMethod>();
		//connection<->battleId
		private Dictionary<long, int> m_connectionIdDict = new Dictionary<long, int>();
		private Handler m_instance = null;
		private int m_tcpObjectId = 0;
		protected int m_serviceAddress = 0;

		public void Init(int address, int tcpObjectId)
		{
			m_serviceAddress = address;
			m_tcpObjectId = tcpObjectId;
			RegisterHandleMethod(MsgID.BATTLE_LOGINBATTLEREQ, OnLoginBattle);
			RegisterHandleMethod(MsgID.BATTLE_SENDLOCKSTEPINFO, OnSendLockStepInfo);
		}

		

		private void RegisterHandleMethod(MsgID msgID, HandleMethod method)
		{
			m_handleMethods.Add(msgID, method);
		}

		protected void PushNetworkPacket(Int64 connectionId, MsgID msgId, ByteString data)
		{
			Netmsg.NetMsg msg = new Netmsg.NetMsg();
			msg.Action = (uint)(Int32)msgId;
			msg.Payload = data;

			NetworkPacket message = new NetworkPacket();
			message.Type = Framework.MessageQueue.SocketMessageType.DATA;
			message.TcpObjectId = m_tcpObjectId;
			message.ConnectionId = connectionId;

			MemoryStream ms = new MemoryStream();
			ms.Position = 0;
			var writer = new BinaryWriter(ms);
			writer.Write(msg.ToByteArray());
			writer.Write(2);
			writer.Write((byte)1);
			writer.Flush();

			List<byte[]> buffList = new List<byte[]>();
			buffList.Add(ms.ToArray());

			message.Buffers = buffList;
			NetworkPacketQueue.GetInstance().Push(message);
		}

		public HandleMethod GetHandleMethod(MsgID msgID)
		{
			HandleMethod method = null;
			m_handleMethods.TryGetValue(msgID, out method);
			return method;
		}

		public void ProcessData(Int64 connectionId, string buffer)
		{
			var param = Convert.FromBase64String(buffer);
			byte[] message = (byte[])param.Take(param.Length - 4).ToArray();
			byte[] sessionByte = (byte[])param.Skip(param.Length - 4).ToArray();
			int index = BitConverter.ToInt32(sessionByte, 0);
			Netmsg.NetMsg netMsg = Netmsg.NetMsg.Parser.ParseFrom(message);
			LoggerHelper.Info(m_serviceAddress, string.Format("Action {0}, {1}", netMsg.Action, index));
			MsgID msgId = (MsgID)netMsg.Action;
			switch (msgId)
			{
				case MsgID.BATTLE_LOGINBATTLEREQ:
					{
						var handleMethod = GetHandleMethod(msgId);
						proto.battle.LoginBattleReq req = proto.battle.LoginBattleReq.Parser.ParseFrom(netMsg.Payload);
						RPCCallback cb = delegate (SSContext context, string method, byte[] param, RPCError error)
						{
							MsgID retMsgID = (MsgID)context.IntegerDict["MsgID"];
							ByteString bs = context.ByteStringDict["ByteString"];
							Int64 connectionId = context.LongDict["ConnectionId"];

							if (error == RPCError.OK)
							{
								PushNetworkPacket(connectionId, MsgID.BATTLE_LOGINBATTLERESP, bs);
							}
							else
							{
								PushNetworkPacket(connectionId, MsgID.BATTLE_LOGINBATTLERESP, null);
								LoggerHelper.Info(m_serviceAddress, Encoding.ASCII.GetString(param));
							}
						};
						handleMethod(connectionId, req, MsgID.BATTLE_LOGINBATTLERESP, cb);
					}
					break;
				case MsgID.BATTLE_SENDLOCKSTEPINFO:
					{
						var handlerMethod = GetHandleMethod(msgId);
						proto.battle.SendLockStepInfo req = proto.battle.SendLockStepInfo.Parser.ParseFrom(netMsg.Payload);
						handlerMethod(connectionId, req, 0, null);
					}
					break;
				default:
					{
						LoggerHelper.Info(m_serviceAddress, String.Format("NetMsg Action {0} Not Exit", netMsg.Action));
					}
					break;
			}

		}

		private void OnLoginBattle(Int64 connectionId, IMessage message, MsgID retMsgID, RPCCallback cb)
		{
			var req = message as proto.battle.LoginBattleReq;
			Int64 uid = req.Uid;
			Int32 aid = req.Aid;
			Int32 battleId = req.BattleId;
			LoggerHelper.Info(m_serviceAddress, String.Format("Login Battle, {0}, {1}, {2}", uid, aid, battleId));

			var resp = new proto.battle.LoginBattleResp();
			resp.Ret = 0;

			if (uid == null || uid <= 0)
			{
				resp.Ret = 1;
				PushNetworkPacket(connectionId, retMsgID, resp.ToByteString());
				return;
			}

			if (aid == null || aid <= 0)
			{
				resp.Ret = 1;
				PushNetworkPacket(connectionId, retMsgID, resp.ToByteString());
				return;
			}

			if (battleId == null || battleId <= 0)
			{
				resp.Ret = 2;
				PushNetworkPacket(connectionId, retMsgID, resp.ToByteString());
				return;
			}

			ServiceContext addr = ServiceSlots.GetInstance().Get(String.Format("BattleField{0}", battleId));
			if (addr == null)
			{
				resp.Ret = 3;
				PushNetworkPacket(connectionId, retMsgID, resp.ToByteString());
				return;
			}

			EnterBattleField enterReq = new EnterBattleField();
			enterReq.Uid = uid;
			enterReq.Aid = aid;
			enterReq.ConnectionId = connectionId;
			enterReq.BattleId = battleId;
			enterReq.TcpObjectId = m_tcpObjectId;

			if (m_connectionIdDict.ContainsKey(connectionId))
			{
				m_connectionIdDict[connectionId] = battleId;
			}
			else
			{
				m_connectionIdDict.Add(connectionId, battleId);
			}

			SSContext context = new SSContext();
			context.ByteStringDict["ByteString"] = resp.ToByteString();
			context.IntegerDict["MsgID"] = (int)retMsgID;
			context.LongDict["ConnectionId"] = connectionId;
			addr.Call(addr.GetId(), "EnterBattleField", enterReq.ToByteArray(), context, cb);
		}

		private void OnSendLockStepInfo(long connectionId, IMessage message, MsgID retMsgID, RPCCallback cb)
		{
			int battleId = 0;
			var lockStepInfo = message as proto.battle.SendLockStepInfo;
			m_connectionIdDict.TryGetValue(connectionId, out battleId);
			if (battleId > 0)
			{
				ServiceContext bf = ServiceSlots.GetInstance().Get(String.Format("BattleField{0}", battleId));
				bf.Send(bf.GetId(), "ReceiveStepInfo", lockStepInfo.ToByteArray());
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Protobuf;
using Hatchery.Battle.Bean;
using Hatchery.Battle.Handler;
using Hatchery.Framework.MessageQueue;
using Hatchery.Framework.Network;
using Hatchery.Framework.ProtoSchema;
using Hatchery.Framework.Service;
using Hatchery.Framework.Utility;

namespace Hatchery.Battle.Gateway
{
	public class HandShake
	{
		public string IP { get; set; }
		public int port { get; set; }
		public Int64 connectionId { get; set; }
		public long time { get; set; }
		public System.Net.Sockets.SocketError status { get; set; }
		public bool login { get; set; }
	}


	public class BattleGateway : Hatchery.Framework.Service.Gateway.Gateway
	{
		private Dictionary<Int64, HandShake> m_handShakeDict = new Dictionary<Int64, HandShake>();
		private Dictionary<int, User> m_userOnline = new Dictionary<int, User>();
		private Dictionary<Int64, int> m_connectionUser = new Dictionary<Int64, int>();
		private Hatchery.Battle.Handler.Handler m_handler = null;
		protected override void Init(byte[] param)
		{
			base.Init(param);

			m_handler = new Hatchery.Battle.Handler.Handler();
			m_handler.Init(m_serviceAddress, this.GetTcpObjectId());

			Timeout(null, 30 * 1000, OnClearHandShake);
		}

		private void OnClearHandShake(SSContext context, long currentTime)
		{
			List<Int64> m_removeList = new List<Int64>();
			long curTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			foreach (var v in m_handShakeDict)
			{
				Int64 connectionId = v.Key;
				HandShake shake = v.Value;
				if ((curTime - shake.time) >= 5 && shake.login == false && shake.status == System.Net.Sockets.SocketError.IsConnected)
				{
					LoggerHelper.Info(m_serviceAddress, String.Format("remove connection {0}", shake.connectionId));
					m_removeList.Add(shake.connectionId);
				}
			}

			foreach (var v in m_removeList)
			{
				DisconnectMessage message = new DisconnectMessage();
				message.ConnectionId = v;
				message.Type = Framework.MessageQueue.SocketMessageType.Disconnect;
				message.TcpObjectId = this.GetTcpObjectId();
				NetworkPacketQueue.GetInstance().Push(message);

				HandShake shake;
				m_handShakeDict.TryGetValue(v, out shake);
				shake.status = System.Net.Sockets.SocketError.Disconnecting;
			}

			Timeout(null, 5 * 1000, OnClearHandShake);
		}

		protected override void SocketAccept(int source, int session, string method, byte[] param)
		{
			LoggerHelper.Info(m_serviceAddress, "Battle Gateway Accept");
			SocketAccept accept = Framework.ProtoSchema.SocketAccept.Parser.ParseFrom(param);
			Int64 connectionId = accept.Connection;
			HandShake shake = new HandShake();
			shake.IP = accept.Ip;
			shake.port = accept.Port;
			shake.connectionId = connectionId;
			shake.time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
			shake.login = false;
			m_handShakeDict.Add(connectionId, shake);
		}

		protected override void SocketError(int source, int session, string method, byte[] param)
		{
			SocketError socketError = Hatchery.Framework.ProtoSchema.SocketError.Parser.ParseFrom(param);
			var connectionId = socketError.Connection;
			int errorCode = socketError.ErrorCode;
			var errortext = socketError.ErrorText;
			var remoteEndPoint = socketError.RemoteEndPoint;

			switch (errorCode)
			{
				case (int)SessionSocketError.Disconnected:
					{

						int aid = 0;
						m_connectionUser.TryGetValue(connectionId, out aid);

						User user;
						var exist = m_userOnline.TryGetValue(aid, out user);
						if (exist)
						{
							int battleId = user.BattleId;
							var addr = ServiceSlots.GetInstance().Get(String.Format("BattleField{0}", battleId));
							Hatchery.DisconnectBattleField data = new Hatchery.DisconnectBattleField();
							data.ConnectionId = connectionId;
							data.Aid = aid;

							Send(addr.GetId(), "DisconnectBattleField", data.ToByteArray());
						}



						m_connectionUser.Remove(connectionId);
						m_handShakeDict.Remove(connectionId);
					}
					break;
				default:
					break;
			}
		}

		protected override void SocketData(int source, int session, string method, byte[] param)
		{
			LoggerHelper.Info(m_serviceAddress, "BattleGateway SocketData");
			SocketData data = Framework.ProtoSchema.SocketData.Parser.ParseFrom(param);

			var connectionId = data.Connection;
			var content = Convert.FromBase64String(data.Buffer);
			byte[] message = (byte[])content.Take(content.Length - 4).ToArray();
			byte[] indexByte = (byte[])param.Skip(content.Length - 4).ToArray();
			int index = BitConverter.ToInt32(indexByte, 0);

			try
			{
				Netmsg.NetMsg netmsg = Netmsg.NetMsg.Parser.ParseFrom(message);
				MsgID msgId = (MsgID)netmsg.Action;
				switch (msgId)
				{
					case MsgID.BATTLE_LOGINBATTLEREQ:
						{

							HandShake shake;
							var success = m_handShakeDict.TryGetValue(data.Connection, out shake);
							if (success)
							{
								proto.battle.LoginBattleReq req = proto.battle.LoginBattleReq.Parser.ParseFrom(netmsg.Payload);
								var retMsgID = MsgID.USER_LOGINGATERESP;


								var uid = req.Uid;
								var aid = req.Aid;
								var battleId = req.BattleId;
								LoggerHelper.Info(m_serviceAddress, String.Format("LoginGateReq {0}, {1}, {2}", uid, aid, battleId));


								var resp = new proto.battle.LoginBattleResp();
								resp.Ret = 0;

								if (uid == null || uid <= 0)
								{
									resp.Ret = 1;
									NetHandler.PushNetworkPacket(connectionId, this.GetTcpObjectId(), retMsgID, resp.ToByteString());
									return;
								}

								if (aid == null || aid <= 0)
								{
									resp.Ret = 1;
									NetHandler.PushNetworkPacket(connectionId, this.GetTcpObjectId(), retMsgID, resp.ToByteString());
									return;
								}

								if (battleId == null || battleId <= 0)
								{
									resp.Ret = 2;
									NetHandler.PushNetworkPacket(connectionId, this.GetTcpObjectId(), retMsgID, resp.ToByteString());
									return;
								}

								ServiceContext addr = ServiceSlots.GetInstance().Get(String.Format("BattleField{0}", battleId));
								if (addr == null)
								{
									resp.Ret = 3;
									NetHandler.PushNetworkPacket(connectionId, this.GetTcpObjectId(), retMsgID, resp.ToByteString());
									return;
								}

								EnterBattleField enterReq = new EnterBattleField();
								enterReq.Uid = uid;
								enterReq.Aid = aid;
								enterReq.ConnectionId = connectionId;
								enterReq.BattleId = battleId;
								enterReq.TcpObjectId = this.GetTcpObjectId();

								if (m_userOnline.ContainsKey(aid))
								{
									User user;
									m_userOnline.TryGetValue(aid, out user);

									//remove old 
									m_connectionUser.Remove(user.ConnectionId);

									user.ConnectionId = connectionId;
									m_connectionUser.Add(connectionId, aid);
								}
								else
								{
									var user = new User();
									user.Uid = uid;
									user.Aid = aid;
									user.BattleId = battleId;
									user.ConnectionId = connectionId;
									user.TcpObjectId = this.GetTcpObjectId();
									m_userOnline.Add(aid, user);
									m_connectionUser.Add(connectionId, aid);
								}

								SSContext context = new SSContext();
								context.ByteStringDict["ByteString"] = resp.ToByteString();
								context.IntegerDict["MsgID"] = (int)retMsgID;
								context.LongDict["ConnectionId"] = connectionId;

								RPCCallback cb = delegate (SSContext context, string method, byte[] param, RPCError error)
								{
									MsgID retMsgID = (MsgID)context.IntegerDict["MsgID"];
									ByteString bs = context.ByteStringDict["ByteString"];
									Int64 connectionId = context.LongDict["ConnectionId"];

									if (error == RPCError.OK)
									{
										shake.login = true;
										shake.status = System.Net.Sockets.SocketError.IsConnected;

										NetHandler.PushNetworkPacket(connectionId, this.GetTcpObjectId(), retMsgID, bs);
									}
									else
									{
										NetHandler.PushNetworkPacket(connectionId, this.GetTcpObjectId(), retMsgID, bs);
										LoggerHelper.Info(m_serviceAddress, Encoding.ASCII.GetString(param));
									}
								};

								addr.Call(addr.GetId(), "EnterBattleField", enterReq.ToByteArray(), context, cb);
							}
						}
						break;
					default:
						{
							m_handler.ProcessData(connectionId, data.Buffer);
						}
						break;
				}

			}
			catch (Exception e)
			{
				LoggerHelper.Info(m_serviceAddress, String.Format("Exception caught {0}", e));
			}
		}
	}
}
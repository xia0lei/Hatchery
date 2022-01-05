using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Google.Protobuf;
using Hatchery.Framework.ProtoSchema;
using Hatchery.Framework.Service;
using Hatchery.Framework.Utility;
using proto.battle;

namespace Hatchery.Battle.Test
{
	class ClientContext : Hatchery.Framework.Service.ServiceContext
	{
		private Client m_Client = null;
		private int m_session = 0;
		private Dictionary<string, Method> m_socketMethods = new Dictionary<string, Method>();
		protected override void Init()
		{
			base.Init();

			RegisterServiceMethods("InitClient", OnInitClient);
			RegisterServiceMethods("LoginBattle", OnLogin);

			RegisterSocketMethods("SocketData", OnSocketData);

			Thread inputThread = new Thread(inputThreadMethod);
			inputThread.Start();
		}

		private void OnLogin(int source, int session, string method, byte[] param)
		{
			LoginBattleReq req = new LoginBattleReq();
			req.Uid = 111111;
			req.Aid = 111111;
			req.BattleId = 1;

			Netmsg.NetMsg netmsg = new Netmsg.NetMsg();
			netmsg.Action = (uint)MsgID.BATTLE_LOGINBATTLEREQ;
			netmsg.Payload = req.ToByteString();

			session++;
			MemoryStream ms = new MemoryStream();
			ms.Position = 0;
			var writer = new BinaryWriter(ms);
			writer.Write(netmsg.ToByteArray());
			writer.Write(session);
			writer.Flush();
			m_Client.Write(ms.ToArray());
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

		private void OnSocketData(int source, int session, string method, byte[] param)
		{
			SocketData data = SocketData.Parser.ParseFrom(param);
			Netmsg.NetMsg netMsg = Netmsg.NetMsg.Parser.ParseFrom(Convert.FromBase64String(data.Buffer));
			MsgID msgID = (MsgID)netMsg.Action;
			switch (msgID)
			{
				default:
					{

					}
					break;
			}
		}

		private void RegisterSocketMethods(string name, Method method)
		{
			m_socketMethods.Add(name, method);
		}

		private byte[] pack(MsgID msgID, IMessage message)
		{
			Netmsg.NetMsg netMsg = new Netmsg.NetMsg();
			netMsg.Action = (uint)msgID;
			netMsg.Payload = message.ToByteString();
			return netMsg.ToByteArray();
		}

		private void unpack(byte[] bytes)
		{
			Netmsg.NetMsg netMsg = Netmsg.NetMsg.Parser.ParseFrom(bytes);
			var msgID = netMsg.Action;
			var payload = netMsg.Payload;
			switch (msgID)
			{
				default:
					break;
			}
		}

		private void OnInitClient(int source, int session, string method, byte[] param)
		{
			var addr = ServiceSlots.GetInstance().Get("TestClient");
			LoggerHelper.Info(m_serviceAddress, String.Format("init opaque {0}", addr.GetId()));
			m_Client = new Client(addr.GetId(), "127.0.0.1", 8887);
			m_Client.Connect();
		}

		private static void inputThreadMethod()
		{
			string input = Console.ReadLine();
			while (input != "")
			{
				LoggerHelper.Info(0, String.Format("Call Method {0}", input));
				ClientContext addr = (ClientContext)ServiceSlots.GetInstance().Get("TestClient");
				if (addr != null)
				{
					switch (input)
					{
						case "Init":
							{
								Message message = new Message();
								message.Method = "InitClient";
								message.Destination = addr.GetId();
								message.Type = MessageType.ServiceRequest;
								addr.Push(message);
							}
							break;
						case "LoginBattle":
							{
								Message message = new Message();
								message.Method = "LoginBattle";
								message.Destination = addr.GetId();
								message.Type = MessageType.ServiceRequest;
								addr.Push(message);
							}
							break;
						default:
							break;
					}

				}
				else
				{
					LoggerHelper.Info(0, "TestClient not exist");
				}
				input = Console.ReadLine();
			}
		}
	}
}
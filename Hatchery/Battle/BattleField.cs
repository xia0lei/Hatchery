using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Hatchery.Battle.Bean;
using Hatchery.Framework.MessageQueue;
using Hatchery.Framework.Service;
using Hatchery.Framework.Utility;

namespace Hatchery.Battle
{
	public class BattleField : Framework.Service.ServiceContext
	{
		private List<UserData> m_userDataList = new List<UserData>();
		private List<GameUser> m_userList = new List<GameUser>();
		private GameBattle m_gameBattle = null;
		private int m_randomSeed = 0;
		private int m_battleId = 0;
		private int m_totalFrameIndex = 0;
		private Random m_random = null;
		private List<LockInfo> m_curLockStepInfoList = new List<LockInfo>();
		private List<LockInfo> m_totalLockStepInfoList = new List<LockInfo>();
		private int m_arenaId = 0;
		private int m_arenaPos = 0;
		private int m_birthPos = 0;
		protected override void Init()
		{
			base.Init();
			m_random = new Random(new System.DateTime().Millisecond);
			m_randomSeed = m_random.Next();


			RegisterServiceMethods("InitBattleField", OnInitBattleField);
			RegisterServiceMethods("EnterBattleField", OnEnterBattleField);
			RegisterServiceMethods("ReceiveStepInfo", OnReceiveStepInfo);
			RegisterServiceMethods("ReconnectBattle", OnReconnectBattleField);
			RegisterServiceMethods("DisconnectBattleField", OnDisconnectBattleField);
		}

		private bool isAllDisconnteced()
		{
			bool ret = false;
			int count = 0;

			foreach(var user in m_userList)
			{
				if(user.status == GameStatus.Disconnected)
				{
					count++;
				}
			}

			if(count >= m_userList.Count)
			{
				return true;
			}

			return ret;
		}

		private void OnReconnectBattleField(int source, int session, string method, byte[] param)
		{
			ReconnectBattleResp resp  = new ReconnectBattleResp();
			resp.Ret = 0;
			resp.ArenaId = m_gameBattle.gameData.lastArena;
			resp.ArenaPos = m_gameBattle.gameData.lastArenaPos;
			resp.BirthPos = m_gameBattle.gameData.birthPos;
			DoResponse(source, method, resp.ToByteArray(), session);
		}

		private void OnDisconnectBattleField(int source, int session, string method, byte[] param)
		{
			Hatchery.DisconnectBattleField data = Hatchery.DisconnectBattleField.Parser.ParseFrom(param);
			var connectionid = data.ConnectionId;
			var aid = data.Aid;

			var user = GetUser(aid);
			user.status = GameStatus.Disconnected;
			if(isAllDisconnteced())
			{
				Timeout(null, 15*1000, OnExit);
			}
		}

		private void OnExit(SSContext context, long currentTime)
		{
			Hatchery.RemoveBattleField req = new Hatchery.RemoveBattleField();
			req.ServiceId = this.GetId();
			Send("BattleMgr", "RemoveBattleField", req.ToByteArray());
			HatcheryUtility.Remove(this.GetId());
		}

		private void OnReceiveStepInfo(int source, int session, string method, byte[] param)
		{
			proto.battle.SendLockStepInfo sendLockStepInfo = proto.battle.SendLockStepInfo.Parser.ParseFrom(param);
			LoggerHelper.Info(m_serviceAddress, String.Format("Uid {0}", sendLockStepInfo.Info.Uid));
			LockInfo info = new LockInfo();
			info.Uid = sendLockStepInfo.Info.Uid;
			info.Type = sendLockStepInfo.Info.Type;
			info.Value1 = sendLockStepInfo.Info.Value1;
			info.Value2 = sendLockStepInfo.Info.Value2;
			info.Value3 = sendLockStepInfo.Info.Value3;
			m_curLockStepInfoList.Add(info);
		}

		private void OnInitBattleField(int source, int session, string method, byte[] param)
		{
			m_gameBattle = new GameBattle();
			InitBattleFieldReq initReq = InitBattleFieldReq.Parser.ParseFrom(param);
			m_battleId = initReq.BattleId;
			Hatchery.CreateBattleFieldReq req = initReq.Req;
			m_arenaId = req.ArenaId;
			m_arenaPos = req.ArenaPos;
			m_birthPos = req.BirthPos;
			LoggerHelper.Info(m_serviceAddress, String.Format("arenaId {0},arenaPos {1}, birthPos {2}, {3}", m_arenaId, m_arenaPos, m_birthPos, m_battleId));
			foreach (var player in req.Players)
			{
				UserData userData = new UserData();
				userData.uid = player.Uid;
				for (int i = 0; i < player.ElfList.Count; i++)
				{
					var elfList = player.ElfList;
					ElfData elfData = new ElfData();
					elfData.id = elfList[i].Id;
					elfData.cid = elfList[i].Cid;
					elfData.level = elfList[i].Level;
					elfData.starLevel = elfList[i].StarLevel;
					elfData.isEquip = elfList[i].IsEquip;
					elfData.star = elfList[i].Star;
					elfData.elfVo = ElementElfCFG.items[elfData.cid.ToString()];
					userData.elfList.Add(elfData);
				}

				for (int i = 0; i < player.SlotList.Count; i++)
				{
					EquipElfSlotData equipElfSlotData = new EquipElfSlotData();
					var slot = player.SlotList[i];
					equipElfSlotData.slotId = slot.SlotId;
					equipElfSlotData.elementType = slot.ElementType;
					equipElfSlotData.elfId1 = slot.ElfId1;
					equipElfSlotData.elfId2 = slot.ElfId2;
					equipElfSlotData.elfId3 = slot.ElfId3;
					userData.slotList.Add(equipElfSlotData);
				}
				m_userDataList.Add(userData);
			}
			m_gameBattle.StartBattle(m_randomSeed, m_arenaId, m_arenaPos, 0, m_birthPos, m_userDataList);

			InitBattleFieldResp response = new InitBattleFieldResp();
			response.Ret = 0;
			response.Seed = m_randomSeed;
			response.BattleId = m_battleId;
			DoResponse(source, method, response.ToByteArray(), session);
		}

		private GameUser GetUser(Int32 aid)
		{
			foreach (var user in m_userList)
			{
				if (user.Aid == aid)
				{
					return user;
				}
			}
			return null;
		}

		private void OnEnterBattleField(int source, int session, string method, byte[] param)
		{
			var req = EnterBattleField.Parser.ParseFrom(param);
			var user = GetUser(req.Aid);
			if (user != null)
			{
				user.ConnectionId = req.ConnectionId;
				user.BattleId = req.BattleId;
				user.TcpObjectId = req.TcpObjectId;
			}
			else
			{
				user = new GameUser();
				user.ConnectionId = req.ConnectionId;
				user.BattleId = req.BattleId;
				user.TcpObjectId = req.TcpObjectId;
				m_userList.Add(user);
			}
			user.status = GameStatus.Enter;

			if (m_userList.Count() >= m_userDataList.Count())
			{
				LoggerHelper.Info(m_serviceAddress, "GameStart");
				Timeout(null, 30, Update);
			}
			DoResponse(source, method, null, session);
		}



		private void PushNetworkPacket(Int64 connectionId, int tcpObjectId, MsgID msgID, ByteString data)
		{
			Netmsg.NetMsg msg = new Netmsg.NetMsg();
			msg.Action = (uint)(Int32)msgID;
			msg.Payload = data;

			NetworkPacket message = new NetworkPacket();
			message.Type = Framework.MessageQueue.SocketMessageType.DATA;
			message.TcpObjectId = tcpObjectId;
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

		private void Update(SSContext context, long currentTime)
		{
			if (m_gameBattle != null)
			{
				LockStepInfo lockStepInfo = new LockStepInfo();
				lockStepInfo.Number = m_totalFrameIndex;
				for (int i = 0; i < m_curLockStepInfoList.Count; i++)
				{
					LockInfo li = new LockInfo();
					li.Uid = m_curLockStepInfoList[i].Uid;
					li.Type = m_curLockStepInfoList[i].Type;
					li.Value1 = m_curLockStepInfoList[i].Value1;
					li.Value2 = m_curLockStepInfoList[i].Value2;
					li.Value3 = m_curLockStepInfoList[i].Value3;
					lockStepInfo.Info.Add(li);
				}
				m_gameBattle.gameLogicLockStepInfos.Add(m_totalFrameIndex, lockStepInfo);
				m_curLockStepInfoList.Clear();
				m_gameBattle.UpdateLogic();
				SendUpdate(lockStepInfo);

				m_totalFrameIndex++;
			}

			Timeout(null, 30, Update);
		}

		private void SendUpdate(LockStepInfo lockStepInfo)
		{
			proto.battle.RecLockStepInfo recLockStepInfo = new proto.battle.RecLockStepInfo();
			proto.battle.LockStepInfo lsi = new proto.battle.LockStepInfo();
			lsi.Number = lockStepInfo.Number;
			for (int i = 0; i < lockStepInfo.Info.Count; i++)
			{
				proto.battle.LockInfo li = new proto.battle.LockInfo();
				li.Uid = lockStepInfo.Info[i].Uid;
				li.Type = lockStepInfo.Info[i].Type;
				li.Value1 = lockStepInfo.Info[i].Value1;
				li.Value2 = lockStepInfo.Info[i].Value2;
				li.Value3 = lockStepInfo.Info[i].Value3;
				lsi.Info.Add(li);
			}
			recLockStepInfo.LockStepInfos.Add(lsi);

			//LoggerHelper.Info(m_serviceAddress, String.Format("User Count >>> {0}, framIndex {1}", m_userList.Count, m_totalFrameIndex));
			foreach (var user in m_userList)
			{
				if (user.status != GameStatus.Disconnected)
				{
					PushNetworkPacket(user.ConnectionId, user.TcpObjectId, MsgID.BATTLE_RECLOCKSTEPINFO, recLockStepInfo.ToByteString());
				}
			}
		}
	}
}
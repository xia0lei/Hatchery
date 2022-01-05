using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Google.Protobuf;
using Hatchery.Framework.Service;
using Hatchery.Framework.Utility;

namespace Hatchery.Battle
{
	public class BattleManager : Hatchery.Framework.Service.ServiceContext
	{
		private int m_battleFieldIndex = 0;
		private Dictionary<int, int> m_battlefields = new Dictionary<int, int>();
		protected override void Init()
		{
			base.Init();

			CfgFiles.Init();
			LoggerHelper.Info(m_serviceAddress, "Start Load Config");
			foreach (var cfg in CfgFiles.files)
			{
				Stream stream = new MemoryStream(System.IO.File.ReadAllBytes("../../../Battle/ResAss/Config/" + cfg.Key + ".bytes"));
				BinaryReader binaryReader = new BinaryReader(stream);
				cfg.Value.Read(binaryReader);
			}
			LoggerHelper.Info(m_serviceAddress, "End Load Config");
			RegisterServiceMethods("OnCreateBattleField", OnCreateBattleField);
			RegisterServiceMethods("RemoveBattleField", OnRemoveBattleField);
			RegisterServiceMethods("ReconnectBattle", OnReconnectBattle);
		}

		private void OnReconnectBattle(int source, int session, string method, byte[] param)
		{
			ReconnectBattleReq req = ReconnectBattleReq.Parser.ParseFrom(param);
			var battleId = req.BattleId;

			if (session > 0)
			{
				var bf = ServiceSlots.GetInstance().Get(String.Format("BattleField{0}", battleId));
				if (bf == null)
				{
					SSContext context = new SSContext();
					context.IntegerDict["source"] = source;
					context.IntegerDict["session"] = session;
					context.StringDict["RemoteMethod"] = method;
					this.Call(bf.GetId(), "ReconnectBattle", param, context, OnReconnectBattleCallback);
				}
				else
				{
					ReconnectBattleResp resp = new ReconnectBattleResp();
					resp.Ret = 1;
					DoResponse(source, method, resp.ToByteArray(), session);
				}
			}
		}

		private void OnReconnectBattleCallback(SSContext context, string method, byte[] param, RPCError error)
		{
			int source = context.IntegerDict["source"];
			int session = context.IntegerDict["session"];
			string remoteMethod = context.StringDict["RemoteMethod"];
			LoggerHelper.Info(m_serviceAddress, String.Format("session {0}, source {1}", session, source));
			if (error == RPCError.OK)
			{
				DoResponse(source, remoteMethod, param, session);
			}
			else
			{
				DoError(source, session, error, Encoding.ASCII.GetString(param));
			}
		}

		private void OnRemoveBattleField(int source, int session, string method, byte[] param)
		{
			Hatchery.RemoveBattleField req = Hatchery.RemoveBattleField.Parser.ParseFrom(param);
			int serviceId = req.ServiceId;
			int key = 0;
			foreach (var p in m_battlefields)
			{
				if (p.Value == serviceId)
				{
					key = p.Key;
					break;
				}
			}
			m_battlefields.Remove(key);
		}

		private void OnCreateBattleField(int source, int session, string method, byte[] param)
		{
			m_battleFieldIndex++;
			int bfAddr = HatcheryUtility.NewService("Hatchery.Battle.BattleField", String.Format("BattleField{0}", m_battleFieldIndex));
			m_battlefields.Add(m_battleFieldIndex, bfAddr);

			InitBattleFieldReq req = new InitBattleFieldReq();
			req.BattleId = m_battleFieldIndex;
			req.Req = CreateBattleFieldReq.Parser.ParseFrom(param);

			SSContext context = new SSContext();
			LoggerHelper.Info(m_serviceAddress, String.Format("session {0}, source {1}", session, source));
			//source->ClusterServer session->ClusterServer session
			context.IntegerDict["source"] = source;
			context.IntegerDict["session"] = session;
			context.StringDict["RemoteMethod"] = method;
			Call(bfAddr, "InitBattleField", req.ToByteArray(), context, OnCreateBattleFieldCallback);
		}

		private void OnCreateBattleFieldCallback(SSContext context, string method, byte[] param, RPCError error)
		{
			int source = context.IntegerDict["source"];
			int session = context.IntegerDict["session"];
			string remoteMethod = context.StringDict["RemoteMethod"];
			LoggerHelper.Info(m_serviceAddress, String.Format("session {0}, source {1}", session, source));
			if (error == RPCError.OK)
			{
				InitBattleFieldResp resp = InitBattleFieldResp.Parser.ParseFrom(param);
				CreateBattleFieldResp response = new CreateBattleFieldResp();
				response.BattleId = resp.BattleId;
				response.Ret = resp.Ret;
				response.Seed = resp.Seed;
				LoggerHelper.Info(m_serviceAddress, String.Format("InitBattleFieldCB {0}, {1}, {2}, {3}", remoteMethod, response.Ret, response.Seed, response.BattleId));
				DoResponse(source, remoteMethod, response.ToByteArray(), session);
			}
			else
			{
				DoError(source, session, error, Encoding.ASCII.GetString(param));
			}
		}
	}
}
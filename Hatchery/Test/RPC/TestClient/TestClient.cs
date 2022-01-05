using System;
using System.Text;
using Google.Protobuf;
using Hatchery.Framework.Service;
using Hatchery.Framework.Utility;
using TestServer;

namespace Hatchery.Test.RPC.TestClient
{
	class TestClient : ServiceContext
	{
		protected override void Init()
		{
			base.Init();
			Timeout(null, 1*1000, DoRequest);
		}

		private void DoRequest(SSContext context, long currentTime)
		{
			TestServer_OnRequest request = new TestServer_OnRequest();
			request.RequestTime = currentTime;
			request.RequestText = "hello my friend";

			LoggerHelper.Info(m_serviceAddress, string.Format(">>>>>>>>>>>>>>>>Request Call Time:{0} info:{1}", request.RequestTime, request.RequestText));
			RemoteCall("testserver", "RPCTestServer", "OnRequest", request.ToByteArray(), null, DoRequestCallback);

			LoggerHelper.Info(m_serviceAddress, string.Format(">>>>>>>>>>>>>>>>Request Send Time:{0} info:{1}", request.RequestTime, request.RequestText));
			RemoteSend("testserver", "RPCTestServer", "OnRequest", request.ToByteArray());
		}

		private void DoRequestCallback(SSContext context, string method, byte[] param, RPCError error)
		{
			if (error == RPCError.OK)
			{
				TestServer_OnResponse response = TestServer_OnResponse.Parser.ParseFrom(param);
				LoggerHelper.Info(m_serviceAddress, string.Format("<<<<<<<<<<<<<<<<Response OK Time:{0} info:{1}", response.ResponseTime, response.ResponseText));
				Timeout(null, 10*1000, DoRequest);
			}
			else
			{
				LoggerHelper.Info(m_serviceAddress, string.Format("<<<<<<<<<<<<<<<<Response Error code:{0} error text:{1}", (int)error, Encoding.ASCII.GetString(param)));
			}
		}
	}
}
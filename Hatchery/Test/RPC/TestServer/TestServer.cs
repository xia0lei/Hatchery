using System;
using Google.Protobuf;
using Hatchery.Framework.Service;
using Hatchery.Framework.Utility;
using TestServer;

namespace Hatchery.Test.RPC.TestServer
{
	class TestServer : ServiceContext
	{
		protected override void Init()
		{
			base.Init();
			RegisterServiceMethods("OnRequest", OnRequest);
		}

		private void OnRequest(int source, int session, string method, byte[] param)
		{
			TestServer_OnRequest request = TestServer_OnRequest.Parser.ParseFrom(param);
			LoggerHelper.Info(m_serviceAddress, string.Format("request_time:{0} request_text{1}", request.RequestTime, request.RequestText));

			if (session > 0)
			{
				TestServer_OnResponse response = new TestServer_OnResponse();
				response.ResponseTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				response.ResponseText = string.Format("{0}:{1}", request.RequestText, "response");

				DoResponse(source, method, response.ToByteArray(), session);
			}
		}
	}
}
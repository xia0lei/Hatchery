using Google.Protobuf;
using Hatchery.Framework.Service;
using Hatchery.Framework.Utility;
using SkynetMessageReceiver;

namespace Hatchery.Test.RecvSkynetRequest
{
    class SkynetMessageReceiver : ServiceContext
    {
        protected override void Init()
        {
            base.Init();

            RegisterServiceMethods("OnProcessRequest", OnProcessRequest);
        }

        private void OnProcessRequest(int source, int session, string method, byte[] param)
        {
            SkynetMessageReceiver_OnProcessRequest request = SkynetMessageReceiver_OnProcessRequest.Parser.ParseFrom(param);
            LoggerHelper.Info(m_serviceAddress, string.Format("skynet request_count:{0}", request.RequestText));

            if (session > 0)
            {
                SkynetMessageReceiver_OnProcessRequestResponse response = new SkynetMessageReceiver_OnProcessRequestResponse();
                response.RequestCount = request.RequestCount;
                response.RequestText = request.RequestText;

                DoResponse(source, method, response.ToByteArray(), session);
            }
        }
    }
}

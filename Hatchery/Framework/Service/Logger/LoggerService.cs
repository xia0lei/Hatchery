using System;
using System.Text;
using Hatchery.Framework.ProtoSchema;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Hatchery.Framework.Service.Logger
{
    public class LoggerService : ServiceContext
    {
        private NLog.Logger m_logger;
        public LoggerService()
        {
        }

        protected override void Init(byte[] param)
        {
            base.Init();
            Logger_Init loggerInit = Logger_Init.Parser.ParseFrom(param);
            Startup(loggerInit.LoggerPath);
            RegisterServiceMethods("OnLog", OnLog);
        }

        private void Startup(string loggerPath)
        {
            var config = new LoggingConfiguration();
            var logRoot = loggerPath;
            var filePrefix = "log_";
            var fileTarget = new FileTarget("target")
            {
                FileName = logRoot + "logs/${shortdate}/" + filePrefix + "${date:universalTime=false:format=yyyy_MM_dd_HH}.log",
                Layout = "${longdate} ${message}",
                KeepFileOpen = true,
                AutoFlush = true,
            };
            config.AddTarget(fileTarget);
            config.AddRuleForAllLevels(fileTarget);
            LogManager.Configuration = config;
            m_logger = LogManager.GetCurrentClassLogger();
        }

        private void OnLog(int source, int session, string method, byte[] param)
        {
            string outStr = string.Format("[{0:x8}] {1}", source, Encoding.ASCII.GetString(param));
            m_logger.Info(outStr);
            Console.WriteLine("{0}", outStr);
        }
    }
}

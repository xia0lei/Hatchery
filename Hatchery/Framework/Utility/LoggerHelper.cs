using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hatchery.Framework.Service;
using Hatchery.Framework.Service.Logger;

namespace Hatchery.Framework.Utility
{
    class LoggerHelper
    {
        public static void Info(int source, string msg)
        {
            string logger = "logger";
            LoggerService loggerService = (LoggerService)ServiceSlots.GetInstance().Get(logger);

            Message message = new Message();
            message.Method = "OnLog";
            message.Data = Encoding.ASCII.GetBytes(msg);
            message.Destination = loggerService.GetId();
            message.Source = source;
            message.Type = MessageType.ServiceRequest;
            loggerService.Push(message);
        }
    }
}

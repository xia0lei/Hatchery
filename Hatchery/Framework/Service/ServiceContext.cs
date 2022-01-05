using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Google.Protobuf;
using Hatchery.Framework.MessageQueue;
using Hatchery.Framework.ProtoSchema;
using Hatchery.Framework.Timer;
using Hatchery.Framework.Utility;
using Newtonsoft.Json.Linq;

namespace Hatchery.Framework.Service
{
    public delegate void Method(int source, int session, string method, byte[] param);

    public delegate void RPCCallback(SSContext context, string method, byte[] param, RPCError error);

    public delegate void TimeoutCallback(SSContext context, long currentTime);

    public enum RPCError
    {
        OK = 0,
        MethodNotExist = 1,
        SocketDisconnected = 2,
        RemoteError = 3,
        ServiceRuntimeError = 4,
        UnknowRemoteNode = 5
    }

    public class SSContext
    {
        public Dictionary<string, int> IntegerDict = new Dictionary<string, int>();
        public Dictionary<string, long> LongDict = new Dictionary<string, long>();
        public Dictionary<string, float> FloatDict = new Dictionary<string, float>();
        public Dictionary<string, double> DoubleDict = new Dictionary<string, double>();
        public Dictionary<string, string> StringDict = new Dictionary<string, string>();
        public Dictionary<string, bool> BooleanDict = new Dictionary<string, bool>();
        public Dictionary<string, object> ObjectDict = new Dictionary<string, object>();
        public Dictionary<string, ByteString> ByteStringDict = new Dictionary<string, ByteString>();
    }

    public class RPCResponseContext
    {
        public SSContext Context { get; set; }
        public RPCCallback Callback { get; set; }
    }

    public class TimeoutContext
    {
        public TimeoutCallback Callback { get; set; }
        public SSContext Context { get; set; }
    }

    public enum MessageType
    {
        Socket = 1,
        ServiceRequest = 2,
        ServiceResponse = 3,
        Error = 4,
        Timer = 5
    }

    public class Message
    {
        public string Method { get; set; }
        public byte[] Data { get; set; }
        public int Source { get; set; }
        public int Destination { get; set; }
        public int RPCSession { get; set; }
        public MessageType Type { get; set; }

        public override string ToString()
        {
            string str = "";
            str += Source+":";
            str += Method + ":";
            str += (int)Type;
            return str;
        }
    }

    public class ServiceContext
    {
        private Queue<Message> m_messageQueue = new Queue<Message>();
        private SpinLock m_spinLock = new SpinLock();
        private bool m_isInGlobal = false;
        protected int m_loggerId = 0;
        protected int m_serviceAddress = 0;
        protected int m_totalServiceSession = 0;

        private Dictionary<string, Method> m_serviceMethods = new Dictionary<string, Method>();
        private Dictionary<int, RPCResponseContext> m_responseCallbacks = new Dictionary<int, RPCResponseContext>();
        private Dictionary<int, TimeoutContext> m_timeoutCallbacks = new Dictionary<int, TimeoutContext>();

        private JObject m_bootConfig;

        public ServiceContext()
        {
            RegisterServiceMethods("Init",OnInit);
        }

        protected void RegisterServiceMethods(string methodName, Method method)
        {
            m_serviceMethods.Add(methodName, method);
        }

        protected void OnInit(int source, int session, string method, byte[] param)
        {
            string bootConfigText = ConfigHelper.LoadFromFile(HatcheryUtility.GetBootConf());
            m_bootConfig = JObject.Parse(bootConfigText);
            if(param != null)
            {
                Init(param);
            }
            else
            {
                Init();
            }
        }
        
        protected virtual void Init()
        {
            
        }

        protected virtual void Init(byte[] param)
        {

        }

        protected void DoError(int destination, int session, RPCError errorCode, string errorText)
        {
            ErrorMessage msg = new ErrorMessage();
            msg.ErrorCode = (int)errorCode;
            msg.ErrorText = errorText;
            PushToService(destination, "OnError", msg.ToByteArray(), MessageType.Error, session);
        }

        private void PushToService(int destination, string method, byte[] param, MessageType type, int session)
        {
            Message msg = new Message();
            msg.Source = m_serviceAddress;
            msg.Destination = destination;
            msg.Method = method;
            msg.Data = param;
            msg.RPCSession = session;
            msg.Type = type;
            ServiceContext targetService = ServiceSlots.GetInstance().Get(destination);
            targetService.Push(msg);
        }

        public void Push(Message msg)
        {
            bool isLock = false;
            try
            {
                m_spinLock.Enter(ref isLock);
                m_messageQueue.Enqueue(msg);
                if(!m_isInGlobal)
                {
                    GlobalMQ.GetInstance().Push(m_serviceAddress);
                    m_isInGlobal = true;
                }
            }
            finally
            {
                if (isLock)
                {
                    m_spinLock.Exit();
                }
            }
        }

        public Message Pop()
        {
            bool isLock = false;
            Message result = null;
            try
            {
                m_spinLock.Enter(ref isLock);
                if(m_messageQueue.Count> 0)
                {
                    result = m_messageQueue.Dequeue();
                }
                else
                {
                    m_isInGlobal = false;
                }
            }
            finally
            {
                if (isLock)
                {
                    m_spinLock.Exit();
                }
            }
            return result;
        }

        public int GetId()
        {
            return m_serviceAddress;
        }

        public void SetId(int handle)
        {
            m_serviceAddress = handle;
        }

        public void Call(string destination, string method, byte[] param, SSContext context, RPCCallback cb)
        {
            int serviceId = ServiceSlots.GetInstance().Name2Id(destination);
            Call(serviceId, method, param, context, cb);
        }

        public void Call(int destination, string method, byte[] param, SSContext context, RPCCallback cb)
        {
            if (m_totalServiceSession >= Int32.MaxValue)
            {
                m_totalServiceSession = 0;
            }

            int session = ++m_totalServiceSession;
            PushToService(destination, method, param, MessageType.ServiceRequest, session);

            RPCResponseContext responseCallback = new RPCResponseContext();
            responseCallback.Context = context;
            responseCallback.Callback = cb;

            m_responseCallbacks.Add(session, responseCallback);
        }

        public void Send(string destination, string method, byte[] param)
        {
            int serviceId = ServiceSlots.GetInstance().Name2Id(destination);
            Send(serviceId, method, param);
        }

        public void Send(int destination, string method, byte[] param)
        {
            PushToService(destination, method, param, MessageType.ServiceRequest, 0);
        }

        protected virtual void OnSocketCommand(Message msg)
        {

        }

        protected void DoResponse(int destination, string method, byte[] param, int session)
        {
            PushToService(destination, method, param, MessageType.ServiceResponse, session);
        }

        public virtual void Callback(Message msg)
        {
            //LoggerHelper.Info(m_serviceAddress, String.Format("Context Callback, {0}", msg.ToString()));
            try
            {
                switch (msg.Type)
                {
                    case MessageType.ServiceRequest:
                        {
                            OnRequest(msg);
                        }
                        break;
                    case MessageType.ServiceResponse:
                        {
                            OnResponse(msg);
                        }
                        break;
                    case MessageType.Error:
                        {
                            OnError(msg);
                        }
                        break;
                    case MessageType.Socket:
                        {
                            OnSocketCommand(msg);
                        }
                        break;
                    case MessageType.Timer:
                        {
                            OnTimer(msg);
                        }
                        break;
                    default: break;
                }
            }
            catch (Exception e)
            {
                if (msg.Source > 0 && msg.RPCSession > 0)
                {
                    DoError(msg.Source, msg.RPCSession, RPCError.ServiceRuntimeError, "");
                }
                LoggerHelper.Info(m_serviceAddress, e.ToString());
            }
        }

        private void OnRequest(Message msg)
        {
            Method method = null;
            bool isExist = m_serviceMethods.TryGetValue(msg.Method, out method);
            if (isExist)
            {
                method(msg.Source, msg.RPCSession, msg.Method, msg.Data);
            }
            else
            {
                string text = string.Format("Service:{0} has not method {1}", m_serviceAddress, msg.Method);
                LoggerHelper.Info(m_serviceAddress, text);
                DoError(msg.Source, msg.RPCSession, RPCError.MethodNotExist, text);
            }
        }

        private void OnResponse(Message msg)
        {
            RPCResponseContext responseCallback = null;
            bool isExist = m_responseCallbacks.TryGetValue(msg.RPCSession, out responseCallback);
            if (isExist)
            {
                responseCallback.Callback(responseCallback.Context, msg.Method, msg.Data, RPCError.OK);
                m_responseCallbacks.Remove(msg.RPCSession);
            }
            else
            {
                LoggerHelper.Info(m_serviceAddress, string.Format("Service:{0} session:{1} has not response", m_serviceAddress, msg.RPCSession));
            }
        }

        private void OnError(Message msg)
        {

            ErrorMessage error = ErrorMessage.Parser.ParseFrom(msg.Data);

            RPCResponseContext responseCallback = null;
            bool isExist = m_responseCallbacks.TryGetValue(msg.RPCSession, out responseCallback);
            if (isExist)
            {
                responseCallback.Callback(responseCallback.Context, msg.Method, Encoding.ASCII.GetBytes(error.ErrorText), (RPCError)error.ErrorCode);
                m_responseCallbacks.Remove(msg.RPCSession);
            }
            else
            {
                LoggerHelper.Info(m_serviceAddress, string.Format("Service:{0} session:{1} get error:{2}; error text is {3}",
                    m_serviceAddress, msg.RPCSession, error.ErrorCode, error.ErrorText));
            }
        }

         private void RemoteSendDummyCallback(SSContext context, string method, byte[] param, RPCError error)
        {
            // Nothing to do
        }

        protected void RemoteCall(string remoteNode, string service, string method, byte[] param, SSContext context, RPCCallback cb)
        {
            ClusterClientRequest request = new ClusterClientRequest();
            request.RemoteNode = remoteNode;
            request.RemoteService = service;
            request.Method = method;
            request.Param = Convert.ToBase64String(param);

            Call("clusterClient", "Request", request.ToByteArray(), context, cb);
        }

         protected void RemoteSend(string remoteNode, string service, string method, byte[] param)
        {
            ClusterClientRequest request = new ClusterClientRequest();
            request.RemoteNode = remoteNode;
            request.RemoteService = service;
            request.Method = method;
            request.Param = Convert.ToBase64String(param);

            Call("clusterClient", "Request", request.ToByteArray(), null, RemoteSendDummyCallback);
        }

        protected virtual void OnTimer(Message msg)
        {
            TimeoutContext context = null;
            bool isExist = m_timeoutCallbacks.TryGetValue(msg.RPCSession, out context);
            if (isExist)
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                context.Callback(context.Context, timestamp);

                m_timeoutCallbacks.Remove(msg.RPCSession);
            }
        }

        protected void Timeout(SSContext context, long timeout, TimeoutCallback callback)
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (timeout <= 0)
            {
                callback(context, timestamp);
            }
            else
            {
                if (m_totalServiceSession >= Int32.MaxValue)
                {
                    m_totalServiceSession = 0;
                }

                SSTimerNode timerNode = new SSTimerNode();
                timerNode.Opaque = m_serviceAddress;
                timerNode.Session = ++m_totalServiceSession;
                timerNode.TimeoutTimestamp = timestamp + timeout;

                SSTimer.GetInstance().Add(timerNode);

                TimeoutContext timeoutContext = new TimeoutContext();
                timeoutContext.Callback = callback;
                timeoutContext.Context = context;
                m_timeoutCallbacks.Add(timerNode.Session, timeoutContext);
            }
        }

    }


}

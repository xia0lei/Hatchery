using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Hatchery.Framework.Utility;
using Hatchery.Network;

namespace Hatchery.Framework.Network
{
    public delegate void TCPObjectErrorHandle(int opaque, long sessionId, string remoteEndPoint, int errorCode, string errorText);
    public delegate void SessionErrorHandle(int opaque, long sessionId, int errorCode, string errorText);
    public delegate void ReadCompleteHandle(int opaque, long sessionId, byte[] bytes, int packetSize);
    public delegate void AcceptHandle(int opaque, long sessionId, string ip, int port);

    public class TCPServer : TCPObject
    {
        private Socket m_listener;
        private string m_bindIP;
        private int m_bindPort;
        private SocketAsyncEventArgs m_acceptEvent = new SocketAsyncEventArgs();
        private long m_totalSessionId = 0;
        private Dictionary<long, Session> m_sessionDict = new Dictionary<long, Session>();
        private BufferPool m_bufferPool = new BufferPool();
        private TCPObjectErrorHandle m_onErrorHandle;
        private ReadCompleteHandle m_onReadCompleteHandle;
        private AcceptHandle m_onAcceptHandle;

        public void Start(string serverIP, int port, int backlog, int opaque, TCPObjectErrorHandle errorCallback, ReadCompleteHandle readCallback, AcceptHandle acceptCallback)
        {
            TCPSynchronizeContext.GetInstance();
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
            m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_listener.Bind(ipEndPoint);
            m_listener.Listen(backlog);

            m_opaque = opaque;
            m_acceptEvent.Completed += IO_Complete;

            m_onErrorHandle = errorCallback;
            m_onReadCompleteHandle = readCallback;
            m_onAcceptHandle = acceptCallback;

            m_bindIP = serverIP;
            m_bindPort = port;
            BeginAccept();
        }

        private void IO_Complete(object sender, object o)
        {
            SocketAsyncEventArgs asyncEventArgs = o as SocketAsyncEventArgs;
            if(asyncEventArgs.LastOperation == SocketAsyncOperation.Accept)
            {
                TCPSynchronizeContext.GetInstance().Post(OnAcceptComplete, asyncEventArgs);
            }
        }

        private void OnAcceptComplete(object o)
        {
            SocketAsyncEventArgs args = o as SocketAsyncEventArgs;
            if(args.SocketError == SocketError.Success)
            {
                Socket socket = args.AcceptSocket;
                try
                {
                    Session session = new Session();
                    IPEndPoint remoteEndPoint = socket.RemoteEndPoint as IPEndPoint;
                    UserToken userToken = new UserToken();
                    userToken.IP = remoteEndPoint.Address.ToString();
                    userToken.Port = remoteEndPoint.Port;
                    LoggerHelper.Info(0, String.Format("{0} Accept", userToken.ToString()));
                    m_totalSessionId++;

                    session.StartAsServer(socket, m_opaque, m_totalSessionId, remoteEndPoint, m_bufferPool, OnSessionError, m_onReadCompleteHandle, userToken);
                    m_sessionDict.Add(m_totalSessionId, session);
                    m_onAcceptHandle(m_opaque, m_totalSessionId, userToken.IP, userToken.Port);
                }
                catch(Exception e)
                {
                    m_onErrorHandle(m_opaque, 0, "", 0, e.ToString());
                }
            }
            else
            {
                m_onErrorHandle(m_opaque, 0, "", (int)args.SocketError, "");
            }
            BeginAccept();
        }

         private void OnSessionError(int opaque, long sessionId, int errorCode, string errorText)
        {
            string ipEndPoint = "";

            Session session = null;
            m_sessionDict.TryGetValue(sessionId, out session);
            if (session != null)
            {
                IPEndPoint ipEP = session.GetRemoteEndPoint();
                ipEndPoint = ipEP.ToString();

                m_onErrorHandle(opaque, sessionId, ipEndPoint, errorCode, errorText);
                if (errorCode == (int)SessionSocketError.Disconnected)
                {
                    m_sessionDict.Remove(sessionId); 
                }
                else
                {
                    session.Close();
                }
            }
        }

        private void BeginAccept()
        {
            m_acceptEvent.AcceptSocket = null;
            bool willRaiseEvent = m_listener.AcceptAsync(m_acceptEvent);
            if (!willRaiseEvent)
            {
                OnAcceptComplete(m_acceptEvent);
            }
        }

        internal void Loop()
        {
            TCPSynchronizeContext.GetInstance().Loop();
        }

         public override Session GetSessionBy(long sessionId)
        {
            Session session = null;
            m_sessionDict.TryGetValue(sessionId, out session);
            return session;
        }

        public override void Disconnect(long sessionId)
        {
            Session session = GetSessionBy(sessionId);
            if(session != null)
            {
                session.Close();
            }
        }
    }
}

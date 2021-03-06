using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Hatchery.Framework.Utility;
using Hatchery.Network;

namespace Hatchery.Framework.Network
{
    public delegate void ConnectCompleteHandle(int opaque, long sessionId, string ip, int port);
    public class TCPClient : TCPObject
    {
        private int m_totalSessionId = 0;
        private Dictionary<long, Session> m_sessionDict = new Dictionary<long, Session>();
        private BufferPool m_bufferPool = new BufferPool();
        private TCPObjectErrorHandle m_onErrorHandle;
        private ReadCompleteHandle m_onReadCompleteHandle;
        private ConnectCompleteHandle m_onConnectCompleteHandle;


        public void Start(int opaque, TCPObjectErrorHandle errorCallback, ReadCompleteHandle readCallback, ConnectCompleteHandle connectCallback)
        {
            TCPSynchronizeContext.GetInstance();
            m_onErrorHandle = errorCallback;
            m_onReadCompleteHandle = readCallback;
            m_onConnectCompleteHandle = connectCallback;
            m_opaque = opaque;
        }

        public void Stop()
        {
            foreach(KeyValuePair<long, Session> iter in m_sessionDict)
            {
                iter.Value.Close();
            }
        }

        internal void Loop()
        {
            TCPSynchronizeContext.GetInstance().Loop();
        }

        public void Connect(string serverIP, int port)
        {
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            UserToken userToken = new UserToken();
            userToken.IP = serverIP;
            userToken.Port = port;

            m_totalSessionId++;
            Session session = new Session();
            session.StartAsClient(socket, m_opaque, m_totalSessionId, m_bufferPool, ipEndPoint, OnSessionError, m_onReadCompleteHandle, m_onConnectCompleteHandle, userToken);
            m_sessionDict.Add(m_totalSessionId, session);
        }

        private void OnSessionError(int opaque, long sessionId, int errorCode, string errorText)
        {
            string remoteEndPoint = "";

            Session session = null;
            m_sessionDict.TryGetValue(sessionId, out session);
            if (session != null)
            {
                IPEndPoint ipEndPoint = session.GetRemoteEndPoint();
                remoteEndPoint = ipEndPoint.ToString();

                m_onErrorHandle(opaque, sessionId, remoteEndPoint, errorCode, errorText);

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

using System;
using System.Collections.Generic;

namespace Hatchery.Framework.Network
{
    public class TCPObjectContainer
    {
        private Dictionary<int, TCPObject> m_tcpObjectDict = new Dictionary<int, TCPObject>();
        private int m_totalObjectId = 0;
        public TCPObjectContainer()
        {
        }

        public int Add(TCPObject tcpObject)
        {
            int id = ++m_totalObjectId;
            tcpObject.SetObjectId(id);
            m_tcpObjectDict.Add(id, tcpObject);
            return id;
        }

        public TCPObject Get(int id)
        {
            TCPObject tcpObject = null;
            m_tcpObjectDict.TryGetValue(id, out tcpObject);
            return tcpObject;
        }

        public void Remove(int tcpObjectId)
        {
            m_tcpObjectDict.Remove(tcpObjectId);
        }
    }
}

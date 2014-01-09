using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;

namespace RhesusNet.NET
{
    public class NetServer : NetPeer
    {
        public List<NetConnection> Connections
        {
            get { lock (_netConnectionMutex) { return new List<NetConnection>(_netConnections); } }
        }

        public NetServer(int port)
            : base("", port, NetConnectionType.SERVER)
        {
        }

        public void SendToAll(NetBuffer buff, NetChannel method, int channel)
        {
            List<NetConnection> netConnections = Connections;

            foreach (NetConnection nc in netConnections)
            {
                Send(buff, nc, method, channel);
            }
        }

    }
}

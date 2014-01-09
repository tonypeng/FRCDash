using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;

namespace RhesusNet.NET
{
    public class NetConnection
    {
        private Socket _socket;

        private EndPoint _remoteEndpoint;
        private NetPeer _netPeer;

        public EndPoint RemoteEndpoint
        {
            get { return _remoteEndpoint; }
        }

        public NetConnection(EndPoint iep, NetPeer netPeer)
        {
            _remoteEndpoint = iep;

            _netPeer = netPeer;
        }

        public void Send(NetBuffer buff, NetChannel method, int channel)
        {
            _netPeer.Send(buff, this, method, channel);
        }

        public override bool Equals(object obj)
        {
            NetConnection other = (NetConnection)obj;

            return other._remoteEndpoint == _remoteEndpoint;
        }
    }
}

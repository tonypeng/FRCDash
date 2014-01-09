using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;

namespace RhesusNet.NET
{
    public class NetClient : NetPeer
    {
        private NetConnection _server;

        public bool Connected
        {
            get { return _connected; }
        }

        public NetClient()
            : base("0.0.0.0", 0, NetConnectionType.CLIENT)
        {
        }

        public void Connect(EndPoint ep)
        {
            if (_connectionRequested)
                return;

            _connectionRequestTime = DateTime.Now;
            _connectionRequested = true;

            _server = new NetConnection(ep, this);

            NetBuffer netBuffer = new NetBuffer();

            netBuffer.Write((byte)InternalMessageType.LIBRARY_DATA);
            netBuffer.Write((byte)LibraryMessageType.CONNECTION_REQUEST);

            SendRaw(netBuffer, _server);
        }

        public void Disconnect(EndPoint ep)
        {
            if (!(_connected))
            {
                Console.WriteLine("Already Disconnected!");
                return;
            }

            NetBuffer netbuff = new NetBuffer();
            netbuff.Write((byte)InternalMessageType.LIBRARY_DATA);
            netbuff.Write((byte)LibraryMessageType.DISCONNECT_REQUEST);

            SendRaw(netbuff, _server);
           
        }
    }
}

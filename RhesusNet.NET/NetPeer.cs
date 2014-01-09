using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

using RhesusNet.NET;

namespace RhesusNet.NET
{
    public class NetPeer
    {
        public const int SEND_FAILED_BUFFER_ALREADY_SENT = -1000000000;
        public const int SEND_FAILED_BUFFER_INVALID = -1000000001;
        public const long SEND_FAILED_UNKNOWN_ERROR = -10000000002;

        public const int MAX_RECEIVE_BUFFER_SIZE = 1024;
        public const int MAX_MESSAGE_TRACK = 100000;

        private object _reliableUnorderedMutex = new Object();
        private object _reliableSequencedMutex = new Object();
        private object _reliableInOrderMutex = new Object();

        private static double kResendPacketTime = 1.0;

        int _currentReliableUnorderedCounter;
        int _currentReliableSequencedCounter;
        int _currentReliableOrderedCounter;
        int _currentUnreliableSequencedCounter;

        int[] _lastUnreliableSequenced; // int array of length 16 ints - one for each channel 
        int[] _lastReliableSequenced; // int array of length 16 ints - one for each channel

        NetConnectionType _connType;
		
		bool _isRunning;

        Socket _socket;
		//struct sockaddr_in _remote_spec;
        Queue<NetBuffer> _receivedMessages;
        object _receiveQueueMutex = new object();

        Dictionary<int, MessageAwaitingACK>[] _reliableUnordered = new Dictionary<int, MessageAwaitingACK>[16]; // NetBuffer array - one for each subchannel
        Dictionary<int, MessageAwaitingACK>[] _reliableSequenced = new Dictionary<int, MessageAwaitingACK>[16]; // NetBuffer array - one for each subchannel
        Dictionary<int, MessageAwaitingACK>[] _reliableOrdered = new Dictionary<int, MessageAwaitingACK>[16];

        string _ip;
		protected int _port;

        protected IPEndPoint _remoteEndpoint;

        Thread _updateThread;
        Thread _messageCheckThread;

        protected List<NetConnection> _netConnections;
        protected object _netConnectionMutex = new object();

        protected bool _connectionRequested = false;
        protected DateTime _connectionRequestTime = DateTime.Now;
        protected bool _connected = false;

        public NetPeer(string ip, int port, NetConnectionType connType)
        {
	        this._ip = ip;
	        this._port = port;
	        this._connType = connType;
	
	        for(int i = 0; i < 16; i++)
	        {
		        _reliableUnordered[i] = new Dictionary<int, MessageAwaitingACK>();
		        _reliableSequenced[i] = new Dictionary<int, MessageAwaitingACK>();
                _reliableOrdered[i] = new Dictionary<int, MessageAwaitingACK>();
	        }
	
	        this._lastUnreliableSequenced = new int[MAX_MESSAGE_TRACK];
	        this._lastReliableSequenced 	= new int[MAX_MESSAGE_TRACK];
	
	        this._currentReliableUnorderedCounter = 0;
	        this._currentReliableSequencedCounter = 0;
	        this._currentReliableOrderedCounter = 0;
	        this._currentUnreliableSequencedCounter = 0;

            _receivedMessages = new Queue<NetBuffer>();

            _updateThread = new Thread(Update);
            _updateThread.IsBackground = true;

            _messageCheckThread = new Thread(CheckMessages);
            _messageCheckThread.IsBackground = true;

            _netConnections = new List<NetConnection>();
        }

        public void Update()
        {
            while (true)
            {
                int received;
                byte[] rcv_buffer = new byte[MAX_RECEIVE_BUFFER_SIZE];

                EndPoint from = new IPEndPoint(IPAddress.Any, _port);

                try
                {
                    while ((received = _socket.ReceiveFrom(rcv_buffer, ref from)) > 0)
                    {

                        NetBuffer buff = new NetBuffer(rcv_buffer, received);

                        InternalMessageType c = (InternalMessageType)buff.ReadByte();

                        switch (c)
                        {
                            case InternalMessageType.LIBRARY_DATA:
                                byte b = buff.ReadByte();
                                LibraryMessageType msgType = (LibraryMessageType)b;

                                switch (msgType)
                                {
                                    case LibraryMessageType.MESSAGE_ACK:
                                        // first byte is the send type
                                        NetChannel chann = (NetChannel)buff.ReadByte();

                                        // second byte is the channel
                                        int channel = buff.ReadByte();

                                        // 2, 3, 4, 5 (4 bytes) form the packet id
                                        int id = buff.ReadInt32();

                                        switch (chann)
                                        {
                                            case NetChannel.NET_RELIABLE:
                                                lock (_reliableUnorderedMutex)
                                                {
                                                    _reliableUnordered[channel].Remove(id);
                                                }
                                                break;
                                            case NetChannel.NET_RELIABLE_IN_ORDER:
                                                lock (_reliableInOrderMutex)
                                                {
                                                    _reliableOrdered[channel].Remove(id);
                                                }
                                                break;
                                            case NetChannel.NET_RELIABLE_SEQUENCED:
                                                lock (_reliableSequencedMutex)
                                                {
                                                    _reliableSequenced[channel].Remove(id);
                                                }
                                                break;
                                        }
                                        break;
                                    case LibraryMessageType.CONNECTION_REQUEST:
                                        if (_connType == NetConnectionType.CLIENT)
                                        {
                                            Console.WriteLine("Somebody tried to connect to us..."); // wtf?
                                            break;
                                        }

                                        NetConnection nc = new NetConnection(from, this);

                                        // let him connect
                                        lock (_netConnectionMutex)
                                        {
                                            if (!_netConnections.Contains(nc))
                                                _netConnections.Add(nc);
                                        }

                                        NetBuffer confirm = new NetBuffer();

                                        confirm.Write((byte)InternalMessageType.LIBRARY_DATA);
                                        confirm.Write((byte)LibraryMessageType.CONNECTION_CONFIRM);

                                        SendRaw(confirm, nc);
                                        break;
                                    case LibraryMessageType.CONNECTION_CONFIRM:
                                        _connected = true;

                                        Console.WriteLine("Connected!");
                                        break;
                                    case LibraryMessageType.DISCONNECT_REQUEST:
                                        if (_connType == NetConnectionType.CLIENT)
                                        {
                                            Console.WriteLine("Someone Disconnecting from Client?"); //TO-DO: add better way of handling server shutdown
                                            return;
                                        }

                                        NetConnection dcNetChannel = new NetConnection(from, this);

                                        NetBuffer dcConfirm = new NetBuffer();

                                        dcConfirm.Write((byte)LibraryMessageType.DISCONNECT_SERVERCONFIRM);

                                        SendRaw(dcConfirm, dcNetChannel);
                                        break;

                                    case LibraryMessageType.DISCONNECT_SERVERCONFIRM:
                                        if (_connType == NetConnectionType.SERVER)
                                        {
                                            //TO-DO: implement client cleanup functions on disconnect
                                        }
                                        if (_connType == NetConnectionType.CLIENT)
                                        {
                                            Console.WriteLine("Something really went wrong here"); //A client should never be able recieve a DISCONNECT_CONFIRM packet
                                            return;
                                        }
                                        break;

                                    case LibraryMessageType.DISCONNECT_CLIENTCONFIRM:
                                        //Server Cleanup functions                              
                                        break;

                                }
                                break;
                            case InternalMessageType.USER_DATA:
                                {
                                    // read in the library header data first

                                    // first byte is the send type
                                    NetChannel chann = (NetChannel)buff.ReadByte();
                                    // second byte is the channel
                                    int channel = buff.ReadByte();
                                    // 2, 3, 4, 5 (4 bytes) form the packet id
                                    int id = buff.ReadInt32();

                                    // we only need to ACK back reliable packets
                                    switch (chann)
                                    {
                                        case NetChannel.NET_RELIABLE_SEQUENCED:
                                        case NetChannel.NET_RELIABLE_IN_ORDER:
                                        case NetChannel.NET_RELIABLE:
                                            // reliable needs an ACK
                                            NetBuffer ack = new NetBuffer();

                                            ack.Write((byte)InternalMessageType.LIBRARY_DATA);
                                            ack.Write((byte)LibraryMessageType.MESSAGE_ACK);
                                            ack.Write((byte)chann);
                                            ack.Write((byte)channel);
                                            ack.Write(id);

                                            // TODO error handling in the future
                                            //sendto(_socket, ack.GetBuffer(), ack.GetBytePos(), 0, (struct sockaddr*) &_remote_spec, sizeof(_remote_spec));
                                            _socket.SendTo(ack.GetBuffer(), 0, ack.GetBufferLength(), SocketFlags.None, from);

                                            break;
                                    }

                                    bool receive = true;
                                    int lastPacket;

                                    // handle sequencing
                                    switch (chann)
                                    {
                                        case NetChannel.NET_UNRELIABLE_SEQUENCED:
                                            lastPacket = _lastUnreliableSequenced[channel];

                                            if (id <= lastPacket) // TODO: rollover will break this
                                                receive = false;
                                            else
                                                _lastUnreliableSequenced[channel] = id;
                                            break;
                                        case NetChannel.NET_RELIABLE_SEQUENCED:
                                            lastPacket = _lastReliableSequenced[channel];

                                            if (id <= lastPacket) // TODO: rollover will break this
                                            {
                                                receive = false;
                                                Console.WriteLine("Out of sequence packet");
                                            }
                                            else
                                                _lastReliableSequenced[channel] = id;
                                            break;
                                        case NetChannel.NET_RELIABLE_IN_ORDER:
                                            receive = false; // will do this later
                                            break;
                                    }

                                    if (receive)
                                    {
                                        // synchronize on the semaphore so that we make sure we're safely accessing the internal message queue
                                        lock (_receiveQueueMutex)
                                        {
                                            _receivedMessages.Enqueue(buff);
                                        } // release the lock on the queue
                                    }
                                    break;
                                }
                            default:
                                // wtf?
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    // TODO: handle it
                }
            }
        }

        private void CheckMessages()
        {
            while (true)
            {
                for (int channel = 0; channel < 16; channel++)
                {
                    DateTime now;
                    MessageAwaitingACK maack;

                    lock (_reliableUnorderedMutex)
                    {
                        foreach (KeyValuePair<int, MessageAwaitingACK> kvp in _reliableUnordered[channel])
                        {
                            maack = kvp.Value;

                            now = DateTime.Now;

                            if (maack.initialized && !maack.acknowledged && (now - maack.sentTime).TotalSeconds > kResendPacketTime)
                            {
                                // mark this one as received so we don't send duplicates
                                kvp.Value.acknowledged = true;

                                // resend
                                Send(maack.buff, maack.recipient, NetChannel.NET_RELIABLE, channel, kvp.Key);
                            }
                        }
                    }

                    lock (_reliableSequencedMutex)
                    {
                        // check reliable sequenced
                        foreach (KeyValuePair<int, MessageAwaitingACK> kvp in _reliableSequenced[channel])
                        {
                            maack = kvp.Value;

                            // update timestamp
                            now = DateTime.Now;

                            if (maack.initialized && !maack.acknowledged && (now - maack.sentTime).TotalSeconds > kResendPacketTime)
                            {
                                // mark this one as received so we don't send duplicates
                                kvp.Value.acknowledged = true;

                                // resend
                                Send(maack.buff, maack.recipient, NetChannel.NET_RELIABLE_SEQUENCED, channel, kvp.Key);
                            }
                        }
                    }

                    lock (_reliableInOrderMutex)
                    {
                        // check reliable in order
                        foreach (KeyValuePair<int, MessageAwaitingACK> kvp in _reliableOrdered[channel])
                        {
                            maack = kvp.Value;

                            // update timestamp
                            now = DateTime.Now;

                            if (maack.initialized && !maack.acknowledged && (now - maack.sentTime).TotalSeconds > kResendPacketTime)
                            {
                                // mark this one as received so we don't send duplicates
                                kvp.Value.acknowledged = true;

                                // resend
                                Send(maack.buff, maack.recipient, NetChannel.NET_RELIABLE_IN_ORDER, channel, kvp.Key);
                            }
                        }
                    }
                }


                if (_connectionRequested && !_connected)
                {
                    if ((DateTime.Now - _connectionRequestTime).TotalSeconds > 1.00)
                    {
                        Console.WriteLine("Connection timeout.");

                        _connectionRequestTime = DateTime.Now;
                    }
                }
            }
        }

        public NetBuffer ReadMessage()
        {
            NetBuffer outgoing;

            lock (_receiveQueueMutex)
            {
                if (_receivedMessages.Count > 0)
                    outgoing = _receivedMessages.Dequeue();
                else
                    outgoing = null;
            }
            return outgoing;
        }

        public void Open(params SocketOptionName[] socket_options)
        {
	        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

	        int i;
	
	        for(i = 0; i < socket_options.Length; i++)
	        {
                _socket.SetSocketOption(SocketOptionLevel.Socket, socket_options[i], true);
	        }

	        switch(this._connType)
	        {
		        case NetConnectionType.CLIENT:
                    _remoteEndpoint = new IPEndPoint(IPAddress.Parse(this._ip), this._port);
			        break;
		        case NetConnectionType.SERVER:
                    _remoteEndpoint = new IPEndPoint(IPAddress.Any, this._port);
			        break;
	        }

            _socket.Bind(_remoteEndpoint);

	        _isRunning = true;

            _updateThread.Start();
            _messageCheckThread.Start();
        }
        
        public void Close()
        {
	        _isRunning = false;
            #warning this function is potentially bad because we might be killing the thread while it's waiting for a message. -tp
            _updateThread.Abort();

            _socket.Close();
        }

        public void Send(NetBuffer buff, NetConnection to, NetChannel method, int channel, int id = -1)
        {
            if (buff.GetBuffer() == null)
                throw new Exception("Buffer was null!");

            bool addToInternalQueue = id == -1;

            MessageAwaitingACK maack = new MessageAwaitingACK();

            maack.initialized = true;
            maack.buff = buff;
            maack.sentTime = DateTime.Now;
            maack.acknowledged = false;
            maack.recipient = to;

            NetBuffer localBuff = new NetBuffer();

            localBuff.Write((byte)InternalMessageType.USER_DATA);
            localBuff.Write((byte)method);
            localBuff.Write((byte)channel);

            switch (method)
            {
                // TODO: specific handlers for different methods
                case NetChannel.NET_RELIABLE:
                    id = id == -1 ? ++_currentReliableUnorderedCounter : id;

                    localBuff.Write(id);

                    if (addToInternalQueue)
                    {
                        lock (_reliableUnorderedMutex)
                        {
                            _reliableUnordered[channel][id] = maack;
                        }
                    }
                    break;
                case NetChannel.NET_RELIABLE_IN_ORDER:
                    id = id == -1 ? ++_currentReliableOrderedCounter : id;

                    localBuff.Write(id);

                    if (addToInternalQueue)
                    {
                        lock (_reliableInOrderMutex)
                        {
                            _reliableOrdered[channel][id] = maack;
                        }
                    }
                    break;
                case NetChannel.NET_RELIABLE_SEQUENCED:
                    id = id == -1 ? ++_currentReliableSequencedCounter : id;

                    localBuff.Write(id);

                    if (addToInternalQueue)
                    {
                        lock (_reliableSequencedMutex)
                        {
                            _reliableSequenced[channel][id] = maack;
                        }
                    }
                    break;
                case NetChannel.NET_UNRELIABLE:
                    localBuff.Write(0);
                    break;
                case NetChannel.NET_UNRELIABLE_SEQUENCED:
                    localBuff.Write(_currentUnreliableSequencedCounter++);
                    break;
                default:

                    break;
            }

	        localBuff.WriteRaw(buff.GetBuffer(), buff.GetBufferLength());
	
            buff.Sent = true;

            _socket.SendTo(localBuff.GetBuffer(), 0, localBuff.GetBufferLength(), SocketFlags.None, to.RemoteEndpoint);
        }

        protected void SendRaw(NetBuffer nb, NetConnection nc)
        {
            nb.Sent = true;

            _socket.SendTo(nb.GetBuffer(), 0, nb.GetBufferLength(), SocketFlags.None, nc.RemoteEndpoint);
        }
    }
}

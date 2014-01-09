using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using System.Linq;

namespace RhesusNet.NET
{
    public class PacketManager
    {
        static NetClient netconn = new NetClient();
        static NetBuffer b;

        private static Dictionary<string, Queue<NetBuffer>> _idToBufferList = new Dictionary<string, Queue<NetBuffer>>();
        private static Dictionary<byte, List<string>> _headerToIDMap = new Dictionary<byte, List<string>>();

        public static void Start()
        {
            netconn.Open();
            netconn.Connect(new IPEndPoint(IPAddress.Parse("10.8.46.2"), 1140));
         
            while(true)
            {
                while(BufferState())
                {
                    byte header = b.ReadByte();

                    if (!AssertKeyExists(header))
                    {
                        continue;
                        throw new KeyNotFoundException("Unknown message type received.");
                    }

                    if (!AssertBufferKeyExists(_headerToIDMap[header].ToArray()))
                    {
                        continue;
                        throw new KeyNotFoundException("Internal message type mapping is not synchronized.");
                    }

                    for (int i = 0; i < _headerToIDMap[header].Count; i++)
                    {
                        _idToBufferList[_headerToIDMap[header].ElementAt(i)].Enqueue(b);
                    }
                    
                }
            }
        }

        private static bool BufferState()
        {
            b = netconn.ReadMessage();
            return b != null;
        }

        private static bool AssertKeyExists(byte b)
        {
            return _headerToIDMap.ContainsKey(b);
        }

        private static bool AssertBufferKeyExists(params string[] keys)
        {
            for (int i = 0; i < keys.Count() - 1; i++)
            {
                if (!_idToBufferList.ContainsKey(keys[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static void RegisterComponent(string ID, byte header)
        {
                _idToBufferList.Add(ID, new Queue<NetBuffer>());
                if (!_headerToIDMap.ContainsKey(header))
                {
                    _headerToIDMap.Add(header, new List<string>());
                    _headerToIDMap[header].Add(ID);
                }
                else
                {
                    _headerToIDMap[header].Add(ID);
                }
        }

        public static NetBuffer ReadMessage(string ID)
        {
            if (!AssertBufferKeyExists(ID))
                throw new KeyNotFoundException("Key not found.");

            return _idToBufferList[ID].Count > 0 ? _idToBufferList[ID].Dequeue() : null;
        }

    }

}

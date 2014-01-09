using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RhesusNet.NET
{
    public class MessageAwaitingACK
    {
        public bool initialized;

        public NetBuffer buff;
        public DateTime sentTime;

        public bool acknowledged;

        public NetConnection recipient;
    };
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RhesusNet.NET
{
    public enum LibraryMessageType : byte
    {
        MESSAGE_ACK = 0x00,
        CONNECTION_REQUEST = 0x10,
        CONNECTION_CONFIRM = 0x11,
        DISCONNECT_REQUEST = 0x12,
        DISCONNECT_SERVERCONFIRM = 0x13,
        DISCONNECT_CLIENTCONFIRM = 0x14
    }
}

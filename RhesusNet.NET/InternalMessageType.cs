using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RhesusNet.NET
{
    public enum InternalMessageType : byte
    {
        LIBRARY_DATA = 0x00,
        USER_DATA = 0x01,
    }
}

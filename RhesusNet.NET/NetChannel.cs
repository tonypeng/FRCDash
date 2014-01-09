using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RhesusNet.NET
{
    public enum NetChannel : byte
    {
        // UNRELIABLE CHANNELS
        NET_UNRELIABLE = 0x00, //Base channel for an unreliable message.  The message is sent and not tracked

        NET_UNRELIABLE_SEQUENCED = 0x16, //Base channel for an unreliably sequenced message.  If packets arrive out of order, older ones are discarded.

        // RELIABLE CHANNELS
        NET_RELIABLE = 0x32, //Base channel for a reliably sent message.  Packets are guaranteed to arrive at the destination, but order is not guaranteed.

        NET_RELIABLE_SEQUENCED = 0x48, // Base channel for a reliable sequenced message.  Packets are guaranteed to arrive at the destination, but old packets are dropped.

        NET_RELIABLE_IN_ORDER = 0x64,// Base channel for a reliable ordered message.  Packets are guaranteed to arrive at the destination in the order they were sent.
    }

}

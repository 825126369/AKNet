using System;

namespace AKNet.Platform.Socket
{
    [Flags]
    public enum SocketFlags
    {
        // Use no flags for this call.
        None = 0x0000,

        // Process out-of-band data.
        OutOfBand = 0x0001,

        // Peek at incoming message.
        Peek = 0x0002,

        // Send without using routing tables.
        DontRoute = 0x0004,

        // This is defined in the .NET Framework and isn't being ported to .NET Core
        // since it is Windows specific. However, leave this enum value reserved
        // to prevent future numbering conflicts.
        // MaxIOVectorLength = 0x0010

        // Partial send or recv for message.
        Truncated = 0x0100,
        ControlDataTruncated = 0x0200,
        Broadcast = 0x0400,
        Multicast = 0x0800,

        Partial = 0x8000,
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AKNet.Platform.Socket
{
    public enum SocketType:int
    {
        Stream = 1, // stream socket
        Dgram = 2, // datagram socket
        Raw = 3, // raw-protocol interface
        Rdm = 4, // reliably-delivered message
        Seqpacket = 5, // sequenced packet stream
        Unknown = -1, // Unknown socket type
    }
}

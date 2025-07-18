// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AKNet.Platform.Socket
{
    public struct SocketReceiveMessageFromResult
    {
        public int ReceivedBytes;
        public SocketFlags SocketFlags;
        public EndPoint RemoteEndPoint;
        public IPPacketInformation PacketInformation;
    }
}

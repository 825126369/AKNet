// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AKNet.Platform.Socket
{
    // Defines socket option names for the <see cref='System.Net.Sockets.Socket'/> class.
    public enum SocketOptionName
    {
        #region SocketOptionLevel.Socket
        // Record debugging information.
        Debug = 0x0001,

        // Socket is listening.
        AcceptConnection = 0x0002,

        // Allows the socket to be bound to an address that is already in use.
        ReuseAddress = 0x0004,

        // Send keep-alives.
        KeepAlive = 0x0008,

        // Do not route, send directly to interface addresses.
        DontRoute = 0x0010,

        // Permit sending broadcast messages on the socket.
        Broadcast = 0x0020,

        // Bypass hardware when possible.
        UseLoopback = 0x0040,

        // Linger on close if unsent data is present.
        Linger = 0x0080,

        // Receives out-of-band data in the normal data stream.
        OutOfBandInline = 0x0100,

        // Close socket gracefully without lingering.
        DontLinger = ~Linger,

        // Enables a socket to be bound for exclusive access.
        ExclusiveAddressUse = ~ReuseAddress,

        // Specifies the total per-socket buffer space reserved for sends. This is
        // unrelated to the maximum message size or the size of a TCP window.
        SendBuffer = 0x1001,

        // Send low water mark.
        ReceiveBuffer = 0x1002,

        // Specifies the total per-socket buffer space reserved for receives. This is unrelated to the maximum message size or the size of a TCP window.
        SendLowWater = 0x1003,

        // Receive low water mark.
        ReceiveLowWater = 0x1004,

        // Send timeout.
        SendTimeout = 0x1005,

        // Receive timeout.
        ReceiveTimeout = 0x1006,

        // Get error status and clear.
        Error = 0x1007,

        // Get socket type.
        Type = 0x1008,

        // Allow ephemeral port reuse for outbound connections.
        ReuseUnicastPort = 0x3007,

        // Maximum queue length that can be specified by <see cref='System.Net.Sockets.Socket.Listen'/>.
        MaxConnections = 0x7fffffff,
        #endregion

        // The following values are taken from ws2tcpip.h,
        // note that these are understood only by ws2_32.dll and are not backwards compatible
        // with the values found in winsock.h which are understood by wsock32.dll.

        #region SocketOptionLevel.IP
        // IP options.
        IPOptions = 1,

        // Header is included with data.
        HeaderIncluded = 2,

        // IP type of service and preced.
        TypeOfService = 3,

        // IP time to live.
        IpTimeToLive = 4,

        // IP multicast interface.
        MulticastInterface = 9,

        // IP multicast time to live.
        MulticastTimeToLive = 10,

        // IP Multicast loopback.
        MulticastLoopback = 11,

        // Add an IP group membership.
        AddMembership = 12,

        // Drop an IP group membership.
        DropMembership = 13,

        // Don't fragment IP datagrams.
        DontFragment = 14,

        // Join IP group/source.
        AddSourceMembership = 15,

        // Leave IP group/source.
        DropSourceMembership = 16,

        // Block IP group/source.
        BlockSource = 17,

        // Unblock IP group/source.
        UnblockSource = 18,

        // Receive packet information for ipv4.
        PacketInformation = 19,
        #endregion

        #region SocketOptionLevel.IPv6
        HopLimit = 21,

        IPProtectionLevel = 23,

        IPv6Only = 27,
        #endregion

        #region SocketOptionLevel.Tcp
        /// <summary>
        /// Disables the Nagle algorithm for send coalescing.
        /// </summary>
        NoDelay = 1,
        /// <summary>
        /// Use urgent data as defined in RFC-1222. This option can be set only once; after it is set, it cannot be turned off.
        /// </summary>
        BsdUrgent = 2,
        /// <summary>
        /// Use expedited data as defined in RFC-1222. This option can be set only once; after it is set, it cannot be turned off.
        /// </summary>
        Expedited = 2,
        /// <summary>
        /// This enables TCP Fast Open as defined in RFC-7413. The actual observed behavior depend on OS configuration and state of kernel TCP cookie cache.
        /// Enabling TFO can impact interoperability and casue connectivity issues.
        /// </summary>
        FastOpen = 15,
        /// <summary>
        /// The number of TCP keep alive probes that will be sent before the connection is terminated.
        /// </summary>
        TcpKeepAliveRetryCount = 16,
        /// <summary>
        /// The number of seconds a TCP connection will remain alive/idle before keepalive probes are sent to the remote.
        /// </summary>
        TcpKeepAliveTime = 3,
        /// <summary>
        /// The number of seconds a TCP connection will wait for a keepalive response before sending another keepalive probe.
        /// </summary>
        TcpKeepAliveInterval = 17,
        #endregion

        #region SocketOptionlevel.Udp
        NoChecksum = 1,

        // Udp-Lite checksum coverage.
        ChecksumCoverage = 20,

        UpdateAcceptContext = 0x700B,

        UpdateConnectContext = 0x7010,
        #endregion
    }
}

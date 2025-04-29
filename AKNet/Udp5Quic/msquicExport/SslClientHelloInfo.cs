// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;

namespace AKNet.Udp5Quic.Common
{
    public readonly struct SslClientHelloInfo
    {
        public readonly string ServerName { get; }
        public readonly SslProtocols SslProtocols { get; }

        public SslClientHelloInfo(string serverName, SslProtocols sslProtocols)
        {
            ServerName = serverName;
            SslProtocols = sslProtocols;
        }
    }
}

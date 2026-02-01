/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:01
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class QuicDefaults
{
    /// <summary>
    /// <see cref="QuicListenerOptions.ListenBacklog" />.
    /// </summary>
    public const int DefaultListenBacklog = 512;
    /// <summary>
    /// <see cref="QuicClientConnectionOptions" />.<see cref="QuicConnectionOptions.MaxInboundBidirectionalStreams" />.
    /// </summary>
    public const int DefaultClientMaxInboundBidirectionalStreams = 0;
    /// <summary>
    /// <see cref="QuicClientConnectionOptions" />.<see cref="QuicConnectionOptions.MaxInboundUnidirectionalStreams" />.
    /// </summary>
    public const int DefaultClientMaxInboundUnidirectionalStreams = 0;
    /// <summary>
    /// <see cref="QuicServerConnectionOptions" />.<see cref="QuicConnectionOptions.MaxInboundBidirectionalStreams" />.
    /// </summary>
    public const int DefaultServerMaxInboundBidirectionalStreams = 100;
    /// <summary>
    /// <see cref="QuicServerConnectionOptions" />.<see cref="QuicConnectionOptions.MaxInboundUnidirectionalStreams" />.
    /// </summary>
    public const int DefaultServerMaxInboundUnidirectionalStreams = 10;
    /// <summary>
    /// Max value for application error codes that can be sent by QUIC, see <see href="https://www.rfc-editor.org/rfc/rfc9000.html#integer-encoding"/>.
    /// </summary>
    public const long MaxErrorCodeValue = (1L << 62) - 1;

    /// <summary>
    /// Default handshake timeout.
    /// </summary>
    public static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Default initial_max_data value.
    /// </summary>
    public const int DefaultConnectionMaxData = 16 * 1024 * 1024;

    /// <summary>
    /// Default initial_max_stream_data_* value.
    /// </summary>
    public const int DefaultStreamMaxData = 64 * 1024;
}

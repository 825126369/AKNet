/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:49
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net;
using System.Net.Security;

namespace AKNet.Udp1MSQuic.Common
{
    internal sealed class QuicConnectionOptions
    {
        public SslClientAuthenticationOptions ClientAuthenticationOptions { get; set; }
        public EndPoint RemoteEndPoint { get; set; }
        public Action ConnectFinishFunc { get; set; }
        public Action CloseFinishFunc { get; set; }
        public Action<QuicStream> SendFinishFunc { get; set; }
        public Action<QuicStream> ReceiveStreamDataFunc { get; set; }
        public SslServerAuthenticationOptions ServerAuthenticationOptions;
    }
}

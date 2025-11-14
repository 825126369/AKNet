/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:38
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net;
using System.Net.Security;

namespace AKNet.Udp2MSQuic.Common
{
    internal sealed class QuicConnectionOptions
    {
        public SslClientAuthenticationOptions ClientAuthenticationOptions { get; set; }
        public EndPoint RemoteEndPoint { get; set; }
        public Action CloseFinishFunc { get; set; }
        public SslServerAuthenticationOptions ServerAuthenticationOptions;
    }
}

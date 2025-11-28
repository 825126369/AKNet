/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:55
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net;

namespace AKNet.Udp2MSQuic.Common
{
    internal sealed class QuicListenerOptions
    {
        public IPEndPoint ListenEndPoint { get; set; } = null!;
        public int ListenBacklog { get; set; }
        public Func<QuicConnectionOptions> GetConnectionOptionFunc { get; set; } = null!;
    }
}

/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Common
{
    public enum NetType
    {
        TCP,
#if NET9_0_OR_GREATER
        MSQuic,
#endif
        [Obsolete] Udp1Tcp,
        Udp2Tcp,
        Udp3Tcp,

#if DEBUG
        Udp4Tcp,
#endif
    }
}

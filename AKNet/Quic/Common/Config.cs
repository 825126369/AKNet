/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:44
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
#if NET9_0_OR_GREATER
using AKNet.Common;

namespace AKNet.Quic.Common
{
    public class Config
    {
        //Common
        public const bool bUseSocketLock = false;
        public const int nIOContexBufferLength = 1024;
        public const int nDataMaxLength = ushort.MaxValue;
        public const double fReceiveHeartBeatTimeOut = 5.0;
        public const double fMySendHeartBeatMaxTime = 2.0;
        public const double fReConnectMaxCdTime = 3.0;
        public const int MaxPlayerCount = 10000;
    }
}
#endif

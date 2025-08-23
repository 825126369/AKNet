/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Tcp.Common
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

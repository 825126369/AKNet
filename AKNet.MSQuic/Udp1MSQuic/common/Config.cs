/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp1MSQuic.Common
{
    public class Config
    {
        //Common
        public const bool bUseSocketLock = false;
        public const int nIOContexBufferLength = 1024;
        public const int nDataMaxLength = ushort.MaxValue;

        public readonly double fReceiveHeartBeatTimeOut = 5.0;
        public readonly double fMySendHeartBeatMaxTime = 2.0;
        public readonly double fReConnectMaxCdTime = 3.0;

        public readonly int MaxPlayerCount = 10000;
        public readonly string CryptoPasswrod1 = string.Empty;
        public readonly string CryptoPasswrod2 = string.Empty;
    }
}

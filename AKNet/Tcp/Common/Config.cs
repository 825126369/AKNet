/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:24
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Tcp.Common
{
    internal class Config
    {
        //Common
        public const bool bUseSocketLock = false;
        public const int nIOContexBufferLength = 1024;
        public const int nDataMaxLength = ushort.MaxValue;

        public readonly double fReceiveHeartBeatTimeOut = 5.0;
        public readonly double fMySendHeartBeatMaxTime = 2.0;
        public readonly double fReConnectMaxCdTime = 3.0;

        public readonly int MaxPlayerCount = 10000;
        public readonly ECryptoType nECryptoType = ECryptoType.None;
        public readonly string CryptoPasswrod1 = string.Empty;
        public readonly string CryptoPasswrod2 = string.Empty;

        public Config(TcpConfig mUserConfig = null)
        {
            if (mUserConfig != null)
            {
                if (mUserConfig.fMySendHeartBeatMaxTime > 0)
                {
                    fMySendHeartBeatMaxTime = mUserConfig.fMySendHeartBeatMaxTime;
                }
                if (mUserConfig.fReceiveHeartBeatTimeOut > 0)
                {
                    fReceiveHeartBeatTimeOut = mUserConfig.fReceiveHeartBeatTimeOut;
                }
                if (mUserConfig.fReConnectMaxCdTime > 0)
                {
                    fReConnectMaxCdTime = mUserConfig.fReConnectMaxCdTime;
                }
                if (mUserConfig.MaxPlayerCount > 0)
                {
                    MaxPlayerCount = mUserConfig.MaxPlayerCount;
                }

                nECryptoType = mUserConfig.nECryptoType;
                CryptoPasswrod1 = mUserConfig.CryptoPasswrod1;
                CryptoPasswrod2 = mUserConfig.CryptoPasswrod2;
            }
        }

    }
}

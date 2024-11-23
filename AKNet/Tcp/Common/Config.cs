/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:35
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
        public const int nCircularBufferInitCapacity = 1024 * 8;
        
        public readonly int nCircularBufferMaxCapacity = 0;
        public readonly int nMsgPackageBufferMaxLength = 1024 * 8;
        public readonly double fSendHeartBeatMaxTimeOut = 2;
        public readonly double fReceiveHeartBeatMaxTimeOut = 5;
        public readonly double fReceiveReConnectMaxTimeOut = 3;

        //Server
        public readonly int numConnections = 10000;

        //加解密
        public readonly ECryptoType nECryptoType = ECryptoType.None;
        public readonly string password1 = string.Empty;
        public readonly string password2 = string.Empty;

        public Config(TcpConfig TcpConfig = null)
        {
            if (TcpConfig != null)
            {
                nCircularBufferMaxCapacity = TcpConfig.nCircularBufferMaxCapacity;
                nMsgPackageBufferMaxLength = TcpConfig.nMsgPackageBufferMaxLength;
                fSendHeartBeatMaxTimeOut = TcpConfig.fSendHeartBeatMaxTimeOut;
                fReceiveHeartBeatMaxTimeOut = TcpConfig.fReceiveHeartBeatMaxTimeOut;
                fReceiveReConnectMaxTimeOut = TcpConfig.fReceiveReConnectMaxTimeOut;
                numConnections = TcpConfig.numConnections;

                nECryptoType = TcpConfig.nECryptoType;
                password1 = TcpConfig.password1;
                password2 = TcpConfig.password2;
            }
        }

    }
}

/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/7 21:38:42
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Tcp.Common
{
    internal static class Config
    {
        //Common
        public const bool bUseSocketLock = true;
        public const int nPackageFixedHeadSize = 8;
        public const int nIOContexBufferLength = 1024;

        public const int nCircularBufferInitCapacity = 1024 * 8;

        public static readonly int nCircularBufferMaxCapacity = 0;
        public static readonly int nMsgPackageBufferMaxLength = 1024 * 8;
        public static readonly double fSendHeartBeatMaxTimeOut = 2;
        public static readonly double fReceiveHeartBeatMaxTimeOut = 5;
        public static readonly double fReceiveReConnectMaxTimeOut = 3;
        //Server
        public static readonly int numConnections = 10000;

        static Config()
        {
            if (AKNetConfig.TcpConfig != null)
            {
                nCircularBufferMaxCapacity = AKNetConfig.TcpConfig.nCircularBufferMaxCapacity;
                nMsgPackageBufferMaxLength = AKNetConfig.TcpConfig.nMsgPackageBufferMaxLength;
                fSendHeartBeatMaxTimeOut = AKNetConfig.TcpConfig.fSendHeartBeatMaxTimeOut;
                fReceiveHeartBeatMaxTimeOut = AKNetConfig.TcpConfig.fReceiveHeartBeatMaxTimeOut;
                fReceiveReConnectMaxTimeOut = AKNetConfig.TcpConfig.fReceiveReConnectMaxTimeOut;
                numConnections = AKNetConfig.TcpConfig.numConnections;
            }
        }
    }
}

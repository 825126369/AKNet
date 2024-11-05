/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Tcp.Common
{
    internal static class Config
    {
        //Common
        public const int nPackageFixedHeadSize = 8;

        public const int nMsgPackageBufferMaxLength = 1024 * 8;
        public const int nIOContexBufferLength = 1024;
        //Client

        public const double fSendHeartBeatMaxTimeOut = 2.0;
        public const double fReceiveHeartBeatMaxTimeOut = 5.0;
        public const double fReceiveReConnectMaxTimeOut = 2.0;
        //Server
        public const int numConnections = 10000;
    }
}

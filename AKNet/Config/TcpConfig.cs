/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:34
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    public class TcpConfig
    {
        public int nCircularBufferMaxCapacity = 1024 * 64;
        public int nMsgPackageBufferMaxLength = 1024 * 8;
        public double fSendHeartBeatMaxTimeOut = 2.0;
        public double fReceiveHeartBeatMaxTimeOut = 5.0;
        public double fReceiveReConnectMaxTimeOut = 3.0;
        public int numConnections = 10000;
        public ECryptoType nECryptoType = ECryptoType.None;
    }
}

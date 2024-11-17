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
    public interface UdpNetPackageEncryptionInterface
    {
        bool DeEncryption(NetPackage mPackage);
        void Encryption(NetPackage mPackage);
    }

    public class UdpConfig
    {
        public bool bUdpCheck = true;
        public int nUdpCombinePackageInitSize = 1024 * 8; //合并包是可变的
        public int nMsgPackageBufferMaxLength = 1024 * 8;
        public double fReceiveHeartBeatTimeOut = 5.0;
        public double fMySendHeartBeatMaxTime = 2.0;
        public int client_socket_receiveBufferSize = 1024 * 64; //暂时没用到
        public int numConnections = 10000;
        public int server_socket_receiveBufferSize = 1024 * 1024;     //接收缓冲区对丢包影响特别大
        public UdpNetPackageEncryptionInterface NetPackageEncryptionInterface = null;
        internal int nUdpPackagePoolMaxCapacity = 100;
    }
}

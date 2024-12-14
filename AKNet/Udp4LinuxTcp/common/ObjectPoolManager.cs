/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class ObjectPoolManager
    {
        private readonly SafeObjectPool<NetUdpSendFixedSizePackage> mSendPackagePool = null;
        private readonly SafeObjectPool<NetUdpReceiveFixedSizePackage> mReceivePackagePool = null;

        public ObjectPoolManager()
        {
            mSendPackagePool = new SafeObjectPool<NetUdpSendFixedSizePackage>(1024);
            mReceivePackagePool = new SafeObjectPool<NetUdpReceiveFixedSizePackage>(1024);
        }

        public NetUdpSendFixedSizePackage UdpSendPackage_Pop()
        {
            return mSendPackagePool.Pop();
        }

        public void UdpSendPackage_Recycle(NetUdpSendFixedSizePackage mPackage)
        {
            mSendPackagePool.recycle(mPackage);
        }

        public NetUdpReceiveFixedSizePackage UdpReceivePackage_Pop()
        {
            return mReceivePackagePool.Pop();
        }

        public void UdpReceivePackage_Recycle(NetUdpReceiveFixedSizePackage mPackage)
        {
            mReceivePackagePool.recycle(mPackage);
        }

    }
}

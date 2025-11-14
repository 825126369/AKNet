/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:27
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp3Tcp.Common
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

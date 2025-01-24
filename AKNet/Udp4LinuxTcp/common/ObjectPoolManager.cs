/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class ObjectPoolManager
    {
        private readonly SafeObjectPool<NetUdpReceiveFixedSizePackage> mReceivePackagePool = null;

        public ObjectPoolManager()
        {
            mReceivePackagePool = new SafeObjectPool<NetUdpReceiveFixedSizePackage>(1024);
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

/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Buffers;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal class ObjectPoolManager
	{
		private readonly SafeObjectPool<NetUdpFixedSizePackage> mUdpFixedSizePackagePool = null;
        private readonly ArrayPool<byte> mArrayPool = ArrayPool<byte>.Shared;
        public ObjectPoolManager()
        {
            int nMaxCapacity = 0;
           mUdpFixedSizePackagePool = new SafeObjectPool<NetUdpFixedSizePackage>(0, nMaxCapacity);
        }

        public NetUdpFixedSizePackage NetUdpFixedSizePackage_Pop()
        {
           return mUdpFixedSizePackagePool.Pop();
        }

        public void NetUdpFixedSizePackage_Recycle(NetUdpFixedSizePackage mPackage)
        {
           mUdpFixedSizePackagePool.recycle(mPackage);
        }
    }
}

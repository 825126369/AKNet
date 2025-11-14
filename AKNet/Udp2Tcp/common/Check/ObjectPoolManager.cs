/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:26:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Buffers;

namespace AKNet.Udp2Tcp.Common
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
           // return new NetUdpFixedSizePackage();
            return mUdpFixedSizePackagePool.Pop();
        }

        public void NetUdpFixedSizePackage_Recycle(NetUdpFixedSizePackage mPackage)
        {
           mUdpFixedSizePackagePool.recycle(mPackage);
        }
    }
}

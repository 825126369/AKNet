/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal class ObjectPoolManager
	{
		private readonly SafeObjectPool<NetUdpFixedSizePackage> mUdpFixedSizePackagePool = null;
        private readonly SafeObjectPool<NetCombinePackage> mCombinePackagePool = null;
        public ObjectPoolManager()
        {
            mUdpFixedSizePackagePool = new SafeObjectPool<NetUdpFixedSizePackage>();
            mCombinePackagePool = new SafeObjectPool<NetCombinePackage>();
        }

		public void CheckPackageCount()
		{
			NetLog.LogWarning("mUdpFixedSizePackagePool: " + mUdpFixedSizePackagePool.Count());
			NetLog.LogWarning("mCombinePackagePool: " + mCombinePackagePool.Count());
		}

        public NetUdpFixedSizePackage NetUdpFixedSizePackage_Pop()
        {
            return mUdpFixedSizePackagePool.Pop();
        }

        public NetCombinePackage NetCombinePackage_Pop()
        {
            return mCombinePackagePool.Pop();
        }

        public void NetUdpFixedSizePackage_Recycle(NetUdpFixedSizePackage mPackage)
        {
            mUdpFixedSizePackagePool.recycle(mPackage);
        }

        public void NetCombinePackage_Recycle(NetCombinePackage mPackage)
        {
            mCombinePackagePool.recycle(mPackage);
        }

        public void Recycle(NetPackage mPackage)
        {
            if (mPackage is NetUdpFixedSizePackage)
            {
                mUdpFixedSizePackagePool.recycle((NetUdpFixedSizePackage)mPackage);
            }
            else if (mPackage is NetCombinePackage)
            {
                mCombinePackagePool.recycle((NetCombinePackage)mPackage);
            }
            else
            {
                NetLog.Assert(false);
            }
        }
    }
}

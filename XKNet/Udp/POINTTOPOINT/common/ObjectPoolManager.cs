using Google.Protobuf;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Common
{
    internal class ObjectPoolManager
	{
		private readonly SafeObjectPool<NetUdpFixedSizePackage> mUdpFixedSizePackagePool = null;
        private readonly SafeObjectPool<NetCombinePackage> mCombinePackagePool = null;
        private byte[] cacheSendProtobufBuffer = new byte[Config.nMsgPackageBufferMaxLength];
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
            return NetUdpFixedSizePackage_Pop();
        }

        public void NetUdpFixedSizePackage_Recycle(NetUdpFixedSizePackage mPackage)
        {
            mUdpFixedSizePackagePool.recycle(mPackage);
        }

        public void NetCombinePackage_Recycle(NetCombinePackage mPackage)
        {
            mCombinePackagePool.recycle(mPackage);
        }

        public NetCombinePackage NetCombinePackage_Pop()
        {
            return mCombinePackagePool.Pop();
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

        public byte[] EnSureSendBufferOk(IMessage data)
        {
            int Length = data.CalculateSize();
            if (cacheSendProtobufBuffer.Length < Length)
            {
                int newSize = cacheSendProtobufBuffer.Length * 2;
                while (newSize < Length)
                {
                    newSize *= 2;
                }

                cacheSendProtobufBuffer = new byte[newSize];
            }
            return cacheSendProtobufBuffer;
        }
    }
}

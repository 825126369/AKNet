using Google.Protobuf;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Common
{
    internal class ObjectPoolManager : Singleton<ObjectPoolManager>
	{
		public SafeObjectPool<NetUdpFixedSizePackage> mUdpFixedSizePackagePool = null;
		public SafeObjectPool<NetCombinePackage> mCombinePackagePool = null;
        public byte[] cacheSendProtobufBuffer = new byte[Config.nMsgPackageBufferMaxLength];
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
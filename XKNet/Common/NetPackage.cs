using System;

namespace XKNet.Common
{
    public abstract class NetPackage
    {
        public ushort nPackageId = 0;
        public ArraySegment<byte> mBufferSegment = ArraySegment<byte>.Empty;

        public virtual void SetArraySegment()
        {

        }

        public ArraySegment<byte> GetArraySegment()
        {
            return mBufferSegment;
        }
    }


}


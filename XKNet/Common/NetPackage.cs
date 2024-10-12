using System;

namespace XKNet.Common
{
    public abstract class NetPackage
    {
        public ushort nPackageId = 0;
        public ArraySegment<byte> mMsgBuffer = ArraySegment<byte>.Empty;

        public virtual void SetArraySegment()
        {

        }

        public virtual ArraySegment<byte> GetArraySegment()
        {
            return mMsgBuffer;
        }

        public virtual Span<byte> GetMsgSpin()
        {
            return Span<byte>.Empty;
        }
    }


}


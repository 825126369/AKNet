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

        public virtual ReadOnlySpan<byte> GetMsgSpin()
        {
            return new ReadOnlySpan<byte>(mMsgBuffer.Array, mMsgBuffer.Offset, mMsgBuffer.Count);
        }
    }


}


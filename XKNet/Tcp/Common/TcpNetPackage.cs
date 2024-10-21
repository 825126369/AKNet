using System;
using XKNet.Common;

namespace XKNet.Tcp.Common
{
    public class TcpNetPackage : NetPackage
    {
        public byte[] mBuffer;
        public int mLength;

        public override ReadOnlySpan<byte> GetBuff()
        {
            return new ReadOnlySpan<byte>(mBuffer, 0, mLength);
        }

        public override ReadOnlySpan<byte> GetBuffBody()
        {
            return new ReadOnlySpan<byte>(mBuffer, Config.nPackageFixedHeadSize, mLength - Config.nPackageFixedHeadSize);
        }

        public override ReadOnlySpan<byte> GetBuffHead()
        {
            return new ReadOnlySpan<byte>(mBuffer, 0, Config.nPackageFixedHeadSize);
        }
    }
}


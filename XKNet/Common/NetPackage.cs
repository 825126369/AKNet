using System;

namespace XKNet.Common
{
    public abstract class NetPackage
    {
        public ushort nPackageId = 0;
        public abstract ReadOnlySpan<byte> GetBuffBody();
        public abstract ReadOnlySpan<byte> GetBuffHead();
        public abstract ReadOnlySpan<byte> GetBuff();
    }
}


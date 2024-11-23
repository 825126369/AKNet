using System;

namespace AKNet.Common
{
    public enum ECryptoType
    {
        None = 0,
        Xor = 1,
        Aes,
    }

    internal interface NetPackageCryptoInterface
    {
        public ReadOnlySpan<byte> Encode(ReadOnlySpan<byte> input);
        public ReadOnlySpan<byte> Decode(ReadOnlySpan<byte> input);
    }
}

using System;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_HASH
    {
        public int SaltLength;
        public byte[] Salt;
    }

    internal static partial class MSQuicFunc
    {
        static ulong CxPlatHashCreate(CXPLAT_HASH_TYPE HashType, byte[] Salt, int SaltLength, ref CXPLAT_HASH NewHash)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            CXPLAT_HASH Hash = new CXPLAT_HASH();
            if (Hash == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            Hash.SaltLength = SaltLength;
            Array.Copy(Salt, 0, Hash.Salt, 0, SaltLength);
            NewHash = Hash;
        Exit:
            return Status;
        }
    }
}

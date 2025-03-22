namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        static bool BoolOk(long q)
        {
            return q != 0;
        }

        static bool orBufferEqual(byte[] buffer1, byte[] buffer2, int nLength)
        {
            if (buffer1.Length < nLength) return false;
            if (buffer2.Length < nLength) return false;

            for (int i = 0; i < nLength; i++)
            {
                if (buffer1[i] != buffer2[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}

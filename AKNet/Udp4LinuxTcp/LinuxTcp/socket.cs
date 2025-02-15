using AKNet.Common;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class msghdr
    {
        public readonly AkCircularBuffer mBuffer;
        public int nLength;
        public readonly int nMaxLength = 1500;

        public msghdr(AkCircularBuffer buffer, int nMaxLength)
        {
            this.mBuffer = buffer;
            this.nMaxLength = nMaxLength;
        }
    }
}

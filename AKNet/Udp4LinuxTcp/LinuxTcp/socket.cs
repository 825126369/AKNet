using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class msghdr
    {
        public readonly byte[] mBuffer = new byte[1500];
        public int nLength;

        public int MaxLength
        {
            get { return mBuffer.Length; }
        }
    }
}

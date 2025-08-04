using System;

namespace AKNet.Common
{
    internal class AkCircularBuffer2
    {
        public class BufferItem
        {
            public byte[] mBuffer;
            public Memory<byte> mBufferMemory;
            public int nOffset;
            public int nLength;
        }

        private BufferItem[] mItemList;
        private const int BlockSize = 16 * 1024;
        private int nBeginBlockIndex;
        private int nEndBlockIndex;
        private int nRemainBlockCount = 0;

        public AkCircularBuffer2(int initialBufferSize)
        {
            NetLog.Assert(initialBufferSize >= 0);
        }

        public void ResizeItemList()
        {

        }

        public void WriteFrom(ReadOnlySpan<byte> buffer, int offset, int length)
        {
            
        }

        public void WriteTo(ReadOnlySpan<byte> buffer, int offset, int length)
        {

        }

    }
}

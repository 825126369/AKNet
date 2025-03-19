namespace AKNet.Udp5Quic.Common
{
    internal enum QUIC_RECV_BUF_MODE
    {
        QUIC_RECV_BUF_MODE_SINGLE,      // Only one receive with a single contiguous buffer at a time.
        QUIC_RECV_BUF_MODE_CIRCULAR,    // Only one receive that may indicate two contiguous buffers at a time.
        QUIC_RECV_BUF_MODE_MULTIPLE,    // Multiple independent receives that may indicate up to two contiguous buffers at a time.
        QUIC_RECV_BUF_MODE_APP_OWNED    // Uses memory buffers provided by the app. Only one receive at a time,
    }

    internal class QUIC_RECV_CHUNK
    {
        public CXPLAT_LIST_ENTRY_QUIC_RECV_BUFFER Link;
        public byte AllocLength;
        public byte ExternalReference;
        public byte AppOwnedBuffer;
        public byte Buffer;
    }

    internal class QUIC_RECV_BUFFER
    {
        public CXPLAT_LIST_ENTRY Chunks;
        public CXPLAT_POOL AppBufferChunkPool;
        public QUIC_RECV_CHUNK PreallocatedChunk;
        public QUIC_RANGE WrittenRanges;
        public long ReadPendingLength;
        public long BaseOffset;
        public int ReadStart;
        public int ReadLength;
        public int VirtualBufferLength;
        public int Capacity;
        public QUIC_RECV_BUF_MODE RecvMode;
    }

}

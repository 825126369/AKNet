using AKNet.Common;

namespace AKNet.Udp5Quic.Common
{
    internal enum QUIC_RECV_BUF_MODE
    {
        QUIC_RECV_BUF_MODE_SINGLE,      // Only one receive with a single contiguous buffer at a time.
        QUIC_RECV_BUF_MODE_CIRCULAR,    // Only one receive that may indicate two contiguous buffers at a time.
        QUIC_RECV_BUF_MODE_MULTIPLE,    // Multiple independent receives that may indicate up to two contiguous buffers at a time.
        QUIC_RECV_BUF_MODE_APP_OWNED    // Uses memory buffers provided by the app. Only one receive at a time,
    }

    internal class QUIC_RECV_CHUNK : CXPLAT_POOL_Interface<QUIC_RECV_CHUNK>
    {
        public readonly CXPLAT_LIST_ENTRY<QUIC_RECV_CHUNK> Link;
        public int AllocLength;
        public bool ExternalReference;
        public bool AppOwnedBuffer;
        public byte[] Buffer;

        public readonly CXPLAT_POOL_ENTRY<QUIC_RECV_CHUNK> POOL_ENTRY = null;
        public QUIC_RECV_CHUNK()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_RECV_CHUNK>(this);
            Link = new CXPLAT_LIST_ENTRY<QUIC_RECV_CHUNK>(this);
        }
        public CXPLAT_POOL_ENTRY<QUIC_RECV_CHUNK> GetEntry()
        {
            throw new System.NotImplementedException();
        }
        public void Reset()
        {
            throw new System.NotImplementedException();
        }
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

    internal static partial class MSQuicFunc
    {
        static void QuicRecvChunkInitialize(QUIC_RECV_CHUNK Chunk, int AllocLength, byte[] Buffer, bool AppOwnedBuffer)
        {
            Chunk.AllocLength = AllocLength;
            Chunk.Buffer = Buffer;
            Chunk.ExternalReference = false;
            Chunk.AppOwnedBuffer = AppOwnedBuffer;
        }

        static ulong QuicRecvBufferInitialize(QUIC_RECV_BUFFER RecvBuffer, int AllocBufferLength,
            int VirtualBufferLength, QUIC_RECV_BUF_MODE RecvMode,
            CXPLAT_POOL AppBufferChunkPool, QUIC_RECV_CHUNK PreallocatedChunk)
        {
            NetLog.Assert(AllocBufferLength != 0 || RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED);
            NetLog.Assert(VirtualBufferLength != 0 || RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED);
            NetLog.Assert((AllocBufferLength & (AllocBufferLength - 1)) == 0);
            NetLog.Assert((VirtualBufferLength & (VirtualBufferLength - 1)) == 0);
            NetLog.Assert(AllocBufferLength <= VirtualBufferLength);

            RecvBuffer.BaseOffset = 0;
            RecvBuffer.ReadStart = 0;
            RecvBuffer.ReadPendingLength = 0;
            RecvBuffer.ReadLength = 0;
            RecvBuffer.RecvMode = RecvMode;
            RecvBuffer.AppBufferChunkPool = AppBufferChunkPool;
            RecvBuffer.PreallocatedChunk = PreallocatedChunk;
            QuicRangeInitialize(QUIC_MAX_RANGE_ALLOC_SIZE, RecvBuffer.WrittenRanges);
            CxPlatListInitializeHead(RecvBuffer.Chunks);

            if (RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED)
            {
                QUIC_RECV_CHUNK Chunk = null;
                if (PreallocatedChunk != null)
                {
                    Chunk = PreallocatedChunk;
                }
                else
                {
                    Chunk = new QUIC_RECV_CHUNK();
                    if (Chunk == null)
                    {
                        return QUIC_STATUS_OUT_OF_MEMORY;
                    }
                    QuicRecvChunkInitialize(Chunk, AllocBufferLength, (byte[])(Chunk + 1), false);
                }

                CxPlatListInsertHead(RecvBuffer.Chunks, Chunk.Link);
                RecvBuffer.Capacity = AllocBufferLength;
                RecvBuffer.VirtualBufferLength = VirtualBufferLength;
            }
            else
            {
                RecvBuffer.Capacity = 0;
                RecvBuffer.VirtualBufferLength = 0;
            }

            return QUIC_STATUS_SUCCESS;
        }

        static ulong QuicRecvBufferProvideChunks(QUIC_RECV_BUFFER RecvBuffer, CXPLAT_LIST_ENTRY Chunks)
        {
            NetLog.Assert(RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED);
            NetLog.Assert(!CxPlatListIsEmpty(Chunks));

            long NewBufferLength = RecvBuffer.VirtualBufferLength;
            for (CXPLAT_LIST_ENTRY Link = Chunks.Flink; Link != Chunks; Link = Link.Flink)
            {
                QUIC_RECV_CHUNK Chunk = (CXPLAT_LIST_ENTRY_QUIC_RECV_CHUNK)(Link);
                NewBufferLength += Chunk.AllocLength;
            }

            if (NewBufferLength > uint.MaxValue)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            if (CxPlatListIsEmpty(RecvBuffer.Chunks))
            {
                NetLog.Assert(RecvBuffer.ReadStart == 0);
                NetLog.Assert(RecvBuffer.ReadLength == 0);
                QUIC_RECV_CHUNK FirstChunk = CXPLAT_CONTAINING_RECORD(Chunks.Flink, QUIC_RECV_CHUNK, Link);
                RecvBuffer.Capacity = FirstChunk.AllocLength;
            }

            RecvBuffer.VirtualBufferLength = NewBufferLength;
            CxPlatListMoveItems(Chunks, RecvBuffer.Chunks);
            return QUIC_STATUS_SUCCESS;
        }

        static void QuicRecvBufferUninitialize(QUIC_RECV_BUFFER RecvBuffer)
        {
            QuicRangeUninitialize(RecvBuffer.WrittenRanges);
            while (!CxPlatListIsEmpty(RecvBuffer.Chunks))
            {
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD(CxPlatListRemoveHead(RecvBuffer.Chunks));
                QuicRecvChunkFree(RecvBuffer, Chunk);
            }
        }

        static long QuicRecvBufferGetTotalLength(QUIC_RECV_BUFFER RecvBuffer)
        {
            long TotalLength = 0;
            if (QuicRangeGetMaxSafe(RecvBuffer.WrittenRanges, TotalLength))
            {
                TotalLength++;
            }
            NetLog.Assert(TotalLength >= RecvBuffer.BaseOffset);
            return TotalLength;
        }

        static void QuicRecvChunkFree(QUIC_RECV_BUFFER RecvBuffer, QUIC_RECV_CHUNK Chunk)
        {
            if (Chunk == RecvBuffer.PreallocatedChunk)
            {
                return;
            }

            if (Chunk.AppOwnedBuffer)
            {
                CxPlatPoolFree(Chunk);
            }
            else
            {
                CXPLAT_FREE(Chunk, QUIC_POOL_RECVBUF);
            }
        }

        static ulong QuicRecvBufferInitialize(QUIC_RECV_BUFFER RecvBuffer, int AllocBufferLength,int VirtualBufferLength, 
            QUIC_RECV_BUF_MODE RecvMode, QUIC_RECV_CHUNK PreallocatedChunk)
        {
            ulong Status;
            NetLog.Assert(AllocBufferLength != 0 && (AllocBufferLength & (AllocBufferLength - 1)) == 0);       // Power of 2
            NetLog.Assert(VirtualBufferLength != 0 && (VirtualBufferLength & (VirtualBufferLength - 1)) == 0); // Power of 2
            NetLog.Assert(AllocBufferLength <= VirtualBufferLength);

            QUIC_RECV_CHUNK Chunk = null;
            if (PreallocatedChunk != null)
            {
                RecvBuffer.PreallocatedChunk = PreallocatedChunk;
                Chunk = PreallocatedChunk;
            }
            else
            {
                RecvBuffer.PreallocatedChunk = null;
                Chunk = new QUIC_RECV_CHUNK();
                if (Chunk == null)
                {
                    Status = QUIC_STATUS_OUT_OF_MEMORY;
                    goto Error;
                }
            }

            QuicRangeInitialize(QUIC_MAX_RANGE_ALLOC_SIZE, RecvBuffer.WrittenRanges);
            CxPlatListInitializeHead(RecvBuffer.Chunks);
            CxPlatListInsertHead(RecvBuffer.Chunks, Chunk.Link);
            Chunk.AllocLength = AllocBufferLength;
            Chunk.ExternalReference = false;
            RecvBuffer.BaseOffset = 0;
            RecvBuffer.ReadStart = 0;
            RecvBuffer.ReadPendingLength = 0;
            RecvBuffer.ReadLength = 0;
            RecvBuffer.Capacity = AllocBufferLength;
            RecvBuffer.VirtualBufferLength = VirtualBufferLength;
            RecvBuffer.RecvMode = RecvMode;
            Status = QUIC_STATUS_SUCCESS;

        Error:
            return Status;
        }
    }

}

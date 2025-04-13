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
        public long VirtualBufferLength;
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
            CXPLAT_POOL<QUIC_RECV_CHUNK> AppBufferChunkPool, QUIC_RECV_CHUNK PreallocatedChunk)
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
                    QuicRecvChunkInitialize(Chunk, AllocBufferLength, Chunk.Buffer, false);
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
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Link);
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
                QUIC_RECV_CHUNK FirstChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Chunks.Flink);
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
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(CxPlatListRemoveHead(RecvBuffer.Chunks));
                if (Chunk != RecvBuffer.PreallocatedChunk)
                {
                    Chunk = null;
                }
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

        static void QuicRecvBufferRead(QUIC_RECV_BUFFER RecvBuffer, ref long BufferOffset, ref long BufferCount, QUIC_BUFFER[] Buffers)
        {
            NetLog.Assert(QuicRangeGetSafe(RecvBuffer.WrittenRanges, 0) != null);
            NetLog.Assert(!CxPlatListIsEmpty(RecvBuffer.Chunks));
            NetLog.Assert(RecvBuffer.ReadPendingLength == 0 || RecvBuffer.RecvMode ==  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE);
            NetLog.Assert(RecvBuffer.Chunks.Flink.Flink == RecvBuffer.Chunks || RecvBuffer.RecvMode ==  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE);
            
            QUIC_SUBRANGE FirstRange = QuicRangeGet(RecvBuffer.WrittenRanges, 0);
            NetLog.Assert(FirstRange.Low == 0 || FirstRange.Count > RecvBuffer.BaseOffset);
            long ContiguousLength = FirstRange.Count - RecvBuffer.BaseOffset;

            if (RecvBuffer.RecvMode ==  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE)
            {
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Flink);
                NetLog.Assert(!Chunk.ExternalReference);
                NetLog.Assert(RecvBuffer.ReadStart == 0);
                NetLog.Assert(BufferCount >= 1);
                NetLog.Assert(ContiguousLength <= Chunk.AllocLength);

                BufferCount = 1;
                BufferOffset = RecvBuffer.BaseOffset;
                RecvBuffer.ReadPendingLength += ContiguousLength;
                Buffers[0].Length = ContiguousLength;
                Buffers[0].Buffer = Chunk.Buffer;
                Chunk.ExternalReference = true;
            }
            else if (RecvBuffer.RecvMode ==  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_CIRCULAR)
            {
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Flink);
                NetLog.Assert(!Chunk.ExternalReference);
                NetLog.Assert(BufferCount >= 2);
                NetLog.Assert(ContiguousLength <= Chunk.AllocLength);

                BufferOffset = RecvBuffer.BaseOffset;
                RecvBuffer.ReadPendingLength += ContiguousLength;
                Chunk.ExternalReference = true;

                int ReadStart = RecvBuffer.ReadStart;
                if (ReadStart + ContiguousLength > Chunk.AllocLength)
                {
                    BufferCount = 2;
                    Buffers[0].Length = Chunk.AllocLength - ReadStart;
                    Buffers[0].Buffer = Chunk.Buffer + ReadStart;
                    Buffers[1].Length = ContiguousLength - Buffers[0].Length;
                    Buffers[1].Buffer = Chunk.Buffer;
                }
                else
                {
                    BufferCount = 1;
                    Buffers[0].Length = ContiguousLength;
                    Buffers[0].Buffer = Chunk.Buffer + ReadStart;
                }
            }
            else
            {
                NetLog.Assert(RecvBuffer.ReadPendingLength < ContiguousLength); // Shouldn't call read if there is nothing new to read
                int UnreadLength = ContiguousLength - RecvBuffer.ReadPendingLength;
                NetLog.Assert(UnreadLength > 0);
                
                int ChunkReadOffset = RecvBuffer.ReadPendingLength;
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Flink);
                bool IsFirstChunk = true;
                int ChunkReadLength = RecvBuffer.ReadLength;
                while (ChunkReadLength <= ChunkReadOffset)
                {
                    NetLog.Assert(ChunkReadLength != 0);
                    NetLog.Assert(Chunk.ExternalReference);
                    NetLog.Assert(Chunk.Link.Flink != RecvBuffer.Chunks);
                    ChunkReadOffset -= ChunkReadLength;
                    IsFirstChunk = false;
                    Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Chunk.Link.Flink);
                    ChunkReadLength = Chunk.AllocLength;
                }
                NetLog.Assert(BufferCount >= 3);
                NetLog.Assert(ChunkReadOffset <= int.MaxValue);

                ChunkReadLength -= ChunkReadOffset;
                if (IsFirstChunk)
                {
                    ChunkReadOffset = (RecvBuffer.ReadStart + ChunkReadOffset) % Chunk.AllocLength;
                    NetLog.Assert(ChunkReadLength <= UnreadLength);
                }
                else if (ChunkReadLength > UnreadLength)
                {
                    ChunkReadLength = UnreadLength;
                }

                NetLog.Assert(ChunkReadLength <= Chunk.AllocLength);
                if (ChunkReadOffset + ChunkReadLength > Chunk.AllocLength)
                {
                    BufferCount = 2;
                    Buffers[0].Length = Chunk.AllocLength - ChunkReadOffset);
                    Buffers[0].Buffer = Chunk.Buffer + ChunkReadOffset;
                    Buffers[1].Length = ChunkReadLength - Buffers[0].Length;
                    Buffers[1].Buffer = Chunk.Buffer;
                }
                else
                {
                    BufferCount = 1;
                    Buffers[0].Length = ChunkReadLength;
                    Buffers[0].Buffer = Chunk.Buffer + ChunkReadOffset;
                }
                Chunk.ExternalReference = TRUE;

                if (UnreadLength > ChunkReadLength)
                {
                    NetLog.Assert(Chunk.Link.Flink != RecvBuffer.Chunks);
                    ChunkReadLength = UnreadLength - ChunkReadLength;
                    Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Chunk.Link.Flink);
                    NetLog.Assert(ChunkReadLength <= Chunk.AllocLength);
                    Buffers[BufferCount].Length = ChunkReadLength;
                    Buffers[BufferCount].Buffer = Chunk.Buffer;
                    BufferCount = BufferCount + 1;
                    Chunk.ExternalReference = true;
                }

                BufferOffset = RecvBuffer.BaseOffset + RecvBuffer.ReadPendingLength;
                RecvBuffer.ReadPendingLength += UnreadLength;
#if DEBUG
                long TotalBuffersLength = 0;
                for (int i = 0; i < BufferCount; ++i)
                {
                    TotalBuffersLength += Buffers[i].Length;
                }
                NetLog.Assert(TotalBuffersLength <= RecvBuffer.ReadPendingLength);
#endif
            }
        }

        static bool QuicRecvBufferDrain(QUIC_RECV_BUFFER RecvBuffer, int DrainLength)
        {
            NetLog.Assert(DrainLength <= RecvBuffer.ReadPendingLength);
            if (RecvBuffer.RecvMode !=  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                RecvBuffer.ReadPendingLength = 0;
            }

            QUIC_SUBRANGE FirstRange = QuicRangeGet(&RecvBuffer.WrittenRanges, 0);
            NetLog.Assert(FirstRange);
            NetLog.Assert(FirstRange.Low == 0);

            do
            {
                bool PartialDrain = RecvBuffer.ReadLength > DrainLength;
                if (PartialDrain || (QuicRangeSize(RecvBuffer.WrittenRanges) > 1 && RecvBuffer.BaseOffset + RecvBuffer.ReadLength == FirstRange.Count))
                {
                    QuicRecvBufferPartialDrain(RecvBuffer, DrainLength);
                    return !PartialDrain;
                }

                DrainLength = QuicRecvBufferFullDrain(RecvBuffer, DrainLength);
            } while (DrainLength != 0);

            return true;
        }

        static void QuicRecvBufferPartialDrain(QUIC_RECV_BUFFER RecvBuffer, int DrainLength)
        {
            NetLog.Assert(!CxPlatListIsEmpty(RecvBuffer.Chunks));
            QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Flink);
            NetLog.Assert(Chunk.ExternalReference);

            if (Chunk.Link.Flink != RecvBuffer.Chunks && RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                CxPlatListEntryRemove(Chunk.Link);
                if (Chunk != RecvBuffer.PreallocatedChunk)
                {
                    CXPLAT_FREE(Chunk, QUIC_POOL_RECVBUF);
                }

                NetLog.Assert(!CxPlatListIsEmpty(RecvBuffer.Chunks));
                Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Flink);
                NetLog.Assert(!Chunk.ExternalReference);
                RecvBuffer.ReadStart = 0;
            }

            RecvBuffer.BaseOffset += DrainLength;
            if (DrainLength != 0)
            {
                if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE)
                {
                    NetLog.Assert(RecvBuffer.ReadStart == 0);
                    Chunk.Buffer = Chunk.Buffer.sl

                    CxPlatMoveMemory(
                        Chunk.Buffer,
                        Chunk.Buffer + DrainLength,
                        (size_t)(Chunk->AllocLength - (uint32_t)DrainLength)); // TODO - Might be able to copy less than the full alloc length

                }
                else
                {
                    RecvBuffer.ReadStart = ((RecvBuffer.ReadStart + DrainLength) % Chunk.AllocLength);
                    if (Chunk.Link.Flink != RecvBuffer.Chunks)
                    {
                        RecvBuffer.Capacity -= DrainLength;
                    }
                }

                NetLog.Assert(RecvBuffer.ReadLength >= DrainLength);
                RecvBuffer.ReadLength -= DrainLength;
            }

            if (RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                Chunk.ExternalReference = false;
            }
            else
            {
                Chunk.ExternalReference = RecvBuffer.ReadPendingLength != DrainLength;
                NetLog.Assert(DrainLength <= RecvBuffer.ReadPendingLength);
                RecvBuffer.ReadPendingLength -= DrainLength;
            }
        }

    }

}

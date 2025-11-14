/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:33
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace MSQuic1
{
    internal enum QUIC_RECV_BUF_MODE
    {
        QUIC_RECV_BUF_MODE_SINGLE,      // Only one receive with a single contiguous buffer at a time.
        QUIC_RECV_BUF_MODE_CIRCULAR,    // Only one receive that may indicate two contiguous buffers at a time.
        QUIC_RECV_BUF_MODE_MULTIPLE,    // Multiple independent receives that may indicate up to two contiguous buffers at a time.
        QUIC_RECV_BUF_MODE_APP_OWNED    // Uses memory buffers provided by the app. Only one receive at a time,
    }

    internal struct QUIC_RECV_CHUNK_ITERATOR
    {
        public QUIC_RECV_CHUNK NextChunk;
        public CXPLAT_LIST_ENTRY IteratorEnd;
        public int StartOffset; // Offset of the first byte to read in the next chunk.
        public int EndOffset;   // Offset of the last byte to read in the next chunk (inclusive!).
    }

    internal class QUIC_RECV_CHUNK : CXPLAT_POOL_Interface<QUIC_RECV_CHUNK>
    {
        public CXPLAT_POOL<QUIC_RECV_CHUNK> mPool;
        public readonly CXPLAT_POOL_ENTRY<QUIC_RECV_CHUNK> POOL_ENTRY = null;
        public readonly CXPLAT_LIST_ENTRY<QUIC_RECV_CHUNK> Link;

        public int AllocLength;      // Allocation size of Buffer
        public bool ExternalReference;
        public bool AppOwnedBuffer;  // Indicates the buffer is managed by the app
        public QUIC_BUFFER Buffer = null;

        public QUIC_RECV_CHUNK(int nInitSize)
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_RECV_CHUNK>(this);
            Link = new CXPLAT_LIST_ENTRY<QUIC_RECV_CHUNK>(this);
            Buffer = new QUIC_BUFFER(nInitSize);
        }

        public QUIC_RECV_CHUNK()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_RECV_CHUNK>(this);
            Link = new CXPLAT_LIST_ENTRY<QUIC_RECV_CHUNK>(this);
        }

        public CXPLAT_POOL_ENTRY<QUIC_RECV_CHUNK> GetEntry()
        {
            return POOL_ENTRY;
        }

        public void Reset()
        {
            this.AllocLength = 0;
            this.ExternalReference = false;
            this.AppOwnedBuffer = false;
            this.Buffer = null;
        }

        public void SetPool(CXPLAT_POOL<QUIC_RECV_CHUNK> mPool)
        {
            this.mPool = mPool;
        }

        public CXPLAT_POOL<QUIC_RECV_CHUNK> GetPool()
        {
            return mPool;
        }
    }

    internal class QUIC_RECV_BUFFER
    {
        public readonly CXPLAT_LIST_ENTRY Chunks = new CXPLAT_LIST_ENTRY<QUIC_RECV_CHUNK>(null);
        public QUIC_RECV_CHUNK PreallocatedChunk;
        public QUIC_RECV_CHUNK RetiredChunk;
        public readonly QUIC_RANGE WrittenRanges = new QUIC_RANGE();
        public int ReadPendingLength;
        public int BaseOffset;
        public int ReadStart;
        public int ReadLength;
        public int VirtualBufferLength;
        public int Capacity;
        public QUIC_RECV_BUF_MODE RecvMode;

        public QUIC_RECV_BUFFER()
        {

        }
    }

    internal static partial class MSQuicFunc
    {
        static int QuicRecvBufferInitialize(QUIC_RECV_BUFFER RecvBuffer, int AllocBufferLength,
            int VirtualBufferLength, QUIC_RECV_BUF_MODE RecvMode, QUIC_RECV_CHUNK PreallocatedChunk)
        {
            NetLog.Assert(AllocBufferLength != 0 || RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED);
            NetLog.Assert(VirtualBufferLength != 0 || RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED);
            NetLog.Assert((AllocBufferLength & (AllocBufferLength - 1)) == 0);     // Power of 2
            NetLog.Assert((VirtualBufferLength & (VirtualBufferLength - 1)) == 0); // Power of 2
            NetLog.Assert(AllocBufferLength <= VirtualBufferLength);

            RecvBuffer.BaseOffset = 0;
            RecvBuffer.ReadStart = 0;
            RecvBuffer.ReadPendingLength = 0;
            RecvBuffer.ReadLength = 0;
            RecvBuffer.RecvMode = RecvMode;
            RecvBuffer.PreallocatedChunk = PreallocatedChunk;
            RecvBuffer.RetiredChunk = null;
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
                    Chunk = new QUIC_RECV_CHUNK(AllocBufferLength);
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

        static void QuicRecvChunkInitialize(QUIC_RECV_CHUNK Chunk, int AllocLength, QUIC_BUFFER Buffer, bool AppOwnedBuffer)
        {
            Chunk.AllocLength = AllocLength;
            Chunk.Buffer = Buffer;
            Chunk.ExternalReference = false;
            Chunk.AppOwnedBuffer = AppOwnedBuffer;
        }

        static int QuicRecvBufferProvideChunks(QUIC_RECV_BUFFER RecvBuffer, CXPLAT_LIST_ENTRY Chunks)
        {
            NetLog.Assert(RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED);
            NetLog.Assert(!CxPlatListIsEmpty(Chunks));

            int NewBufferLength = RecvBuffer.VirtualBufferLength;
            for (CXPLAT_LIST_ENTRY Link = Chunks.Next; Link != Chunks; Link = Link.Next)
            {
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Link);
                NewBufferLength += Chunk.Buffer.Length;
            }

            if (NewBufferLength > int.MaxValue)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            if (CxPlatListIsEmpty(RecvBuffer.Chunks))
            {
                NetLog.Assert(RecvBuffer.ReadStart == 0);
                NetLog.Assert(RecvBuffer.ReadLength == 0);
                QUIC_RECV_CHUNK FirstChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Chunks.Next);
                RecvBuffer.Capacity = FirstChunk.Buffer.Length;
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

        static int QuicRecvBufferGetTotalLength(QUIC_RECV_BUFFER RecvBuffer)
        {
            ulong TotalLength = 0;
            if (QuicRangeGetMaxSafe(RecvBuffer.WrittenRanges, ref TotalLength))
            {
                TotalLength++;
            }
            NetLog.Assert((int)TotalLength >= RecvBuffer.BaseOffset);
            return (int)TotalLength;
        }

        static int QuicRecvBufferReadBufferNeededCount(QUIC_RECV_BUFFER RecvBuffer)
        {
            if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE)
            {
                return 1;
            }

            if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_CIRCULAR)
            {
                return 2;
            }

            if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                return 3;
            }

            QUIC_SUBRANGE FirstRange = QuicRangeGetSafe(RecvBuffer.WrittenRanges, 0);
            if (FirstRange.IsEmpty || FirstRange.Low != 0)
            {
                return 0;
            }

            long ReadableData = FirstRange.Count - RecvBuffer.BaseOffset;
            NetLog.Assert(!CxPlatListIsEmpty(RecvBuffer.Chunks));
            QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
            int DataInChunks = RecvBuffer.Capacity;
            int BufferCount = 1;

            while (ReadableData > DataInChunks)
            {
                Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Chunk.Link.Next);
                DataInChunks += Chunk.AllocLength;
                BufferCount++;
            }
            return BufferCount;
        }

        static void QuicRecvBufferRead(QUIC_RECV_BUFFER RecvBuffer, ref long BufferOffset, ref int BufferCount, QUIC_BUFFER[] Buffers)
        {
            NetLog.Assert(!QuicRangeGetSafe(RecvBuffer.WrittenRanges, 0).IsEmpty);
            NetLog.Assert(!CxPlatListIsEmpty(RecvBuffer.Chunks));
            NetLog.Assert(RecvBuffer.ReadPendingLength == 0 || RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE);

            QUIC_SUBRANGE FirstRange = QuicRangeGet(RecvBuffer.WrittenRanges, 0);
            NetLog.Assert(FirstRange.Low == 0 || (int)FirstRange.Count > RecvBuffer.BaseOffset);
            int ContiguousLength = (int)FirstRange.Count - RecvBuffer.BaseOffset;

            QUIC_RECV_CHUNK_ITERATOR Iterator = QuicRecvBufferGetChunkIterator(RecvBuffer, RecvBuffer.ReadPendingLength);
            int ReadableDataLeft = ContiguousLength - RecvBuffer.ReadPendingLength;
            int CurrentBufferId = 0;

            while (CurrentBufferId < BufferCount && ReadableDataLeft > 0 && 
                QuicRecvChunkIteratorNext(ref Iterator, true, Buffers[CurrentBufferId]))
            {
                if (Buffers[CurrentBufferId].Length > ReadableDataLeft)
                {
                    Buffers[CurrentBufferId].Length = ReadableDataLeft;
                }
                ReadableDataLeft -= Buffers[CurrentBufferId].Length;
                CurrentBufferId++;
            }

            BufferCount = CurrentBufferId;
            BufferOffset = RecvBuffer.BaseOffset + RecvBuffer.ReadPendingLength;
            RecvBuffer.ReadPendingLength = ContiguousLength - ReadableDataLeft;
            
            NetLog.Assert(RecvBuffer.RecvMode ==  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED || ReadableDataLeft == 0);
            NetLog.Assert(RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE || BufferCount <= 1);
            NetLog.Assert(RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_CIRCULAR || BufferCount <= 2);
            NetLog.Assert(RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE || BufferCount <= 3);
            QuicRecvBufferValidate(RecvBuffer);
        }

        static bool QuicRecvBufferDrain(QUIC_RECV_BUFFER RecvBuffer, int DrainLength)
        {
            NetLog.Assert(DrainLength <= RecvBuffer.ReadPendingLength);
            if (RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                RecvBuffer.ReadPendingLength = 0;
            }

            QUIC_SUBRANGE FirstRange = QuicRangeGet(RecvBuffer.WrittenRanges, 0);
            NetLog.Assert(!FirstRange.IsEmpty);
            NetLog.Assert(FirstRange.Low == 0);

            do
            {
                bool PartialDrain = RecvBuffer.ReadLength > DrainLength;
                if (PartialDrain || (QuicRangeSize(RecvBuffer.WrittenRanges) > 1 && RecvBuffer.BaseOffset + RecvBuffer.ReadLength == (int)FirstRange.Count))
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
            QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
            NetLog.Assert(Chunk.ExternalReference);

            if (Chunk.Link.Next != RecvBuffer.Chunks && RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                CxPlatListEntryRemove(Chunk.Link);
                if (Chunk != RecvBuffer.PreallocatedChunk)
                {
                    Chunk = null;
                }

                NetLog.Assert(!CxPlatListIsEmpty(RecvBuffer.Chunks));
                Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
                NetLog.Assert(!Chunk.ExternalReference);
                RecvBuffer.ReadStart = 0;
            }

            RecvBuffer.BaseOffset += DrainLength;
            if (DrainLength != 0)
            {
                if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE)
                {
                    NetLog.Assert(RecvBuffer.ReadStart == 0);
                    for (int i = 0; i < Chunk.AllocLength - DrainLength; i++)
                    {
                        Chunk.Buffer[i] = Chunk.Buffer[i + DrainLength];
                    }
                }
                else
                {
                    RecvBuffer.ReadStart = ((RecvBuffer.ReadStart + DrainLength) % Chunk.AllocLength);
                    if (Chunk.Link.Next != RecvBuffer.Chunks)
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

        static bool QuicRecvBufferHasUnreadData(QUIC_RECV_BUFFER RecvBuffer)
        {
            QUIC_SUBRANGE FirstRange = QuicRangeGetSafe(RecvBuffer.WrittenRanges, 0);
            if (FirstRange.IsEmpty || FirstRange.Low != 0)
            {
                return false;
            }

            NetLog.Assert((int)FirstRange.Count >= RecvBuffer.BaseOffset);
            int ContiguousLength = (int)FirstRange.Count - RecvBuffer.BaseOffset;
            return ContiguousLength > RecvBuffer.ReadPendingLength;
        }

        static void QuicRecvBufferResetRead(QUIC_RECV_BUFFER RecvBuffer)
        {
            NetLog.Assert(RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE);
            NetLog.Assert(!CxPlatListIsEmpty(RecvBuffer.Chunks));
            QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
            Chunk.ExternalReference = false;
            RecvBuffer.ReadPendingLength = 0;
        }

        static int QuicRecvBufferFullDrain(QUIC_RECV_BUFFER RecvBuffer, int DrainLength)
        {
            NetLog.Assert(!CxPlatListIsEmpty(RecvBuffer.Chunks));
            QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
            NetLog.Assert(Chunk.ExternalReference);

            Chunk.ExternalReference = false;
            DrainLength -= RecvBuffer.ReadLength;
            RecvBuffer.ReadStart = 0;
            RecvBuffer.BaseOffset += RecvBuffer.ReadLength;
            if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                RecvBuffer.ReadPendingLength -= RecvBuffer.ReadLength;
            }
            RecvBuffer.ReadLength = (int)((int)QuicRangeGet(RecvBuffer.WrittenRanges, 0).Count - RecvBuffer.BaseOffset);

            if (Chunk.Link.Next == RecvBuffer.Chunks)
            {
                NetLog.Assert(DrainLength == 0, "App drained more than was available!");
                NetLog.Assert(RecvBuffer.ReadLength == 0);
                return 0;
            }

            CxPlatListEntryRemove(Chunk.Link);
            if (Chunk != RecvBuffer.PreallocatedChunk)
            {
                Chunk = null;
            }

            if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
                RecvBuffer.Capacity = Chunk.AllocLength;
                if (Chunk.AllocLength < RecvBuffer.ReadLength)
                {
                    RecvBuffer.ReadLength = Chunk.AllocLength;
                }
            }

            return DrainLength;
        }

        static int QuicRecvBufferWrite(QUIC_RECV_BUFFER RecvBuffer, int WriteOffset, int WriteLength, QUIC_SSBuffer WriteBuffer, ref int WriteLimit, ref bool ReadyToRead)
        {
            NetLog.Assert(WriteLength != 0);
            ReadyToRead = false; // Most cases below aren't ready to read.

            int AbsoluteLength = WriteOffset + WriteLength;
            if (AbsoluteLength <= RecvBuffer.BaseOffset)
            {
                WriteLimit = 0;
                return QUIC_STATUS_SUCCESS;
            }

            if (AbsoluteLength > RecvBuffer.BaseOffset + RecvBuffer.VirtualBufferLength)
            {
                return QUIC_STATUS_BUFFER_TOO_SMALL;
            }


            int CurrentMaxLength = QuicRecvBufferGetTotalLength(RecvBuffer);
            if (AbsoluteLength > CurrentMaxLength)
            {
                if (AbsoluteLength - CurrentMaxLength > WriteLimit)
                {
                    return QUIC_STATUS_BUFFER_TOO_SMALL;
                }
                WriteLimit = AbsoluteLength - CurrentMaxLength;
            }
            else
            {
                WriteLimit = 0;
            }

            if (RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED)
            {
                int AllocLength = QuicRecvBufferGetTotalAllocLength(RecvBuffer);
                if (AbsoluteLength > RecvBuffer.BaseOffset + AllocLength)
                {
                    int NewBufferLength = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Prev).AllocLength << 1;
                    while (AbsoluteLength > RecvBuffer.BaseOffset + NewBufferLength)
                    {
                        NewBufferLength <<= 1;
                    }
                    if (!QuicRecvBufferResize(RecvBuffer, NewBufferLength))
                    {
                        return QUIC_STATUS_OUT_OF_MEMORY;
                    }
                }
            }

            bool WrittenRangesUpdated = false;
            QUIC_SUBRANGE UpdatedRange = QuicRangeAddRange(
                    RecvBuffer.WrittenRanges,
                    (ulong)WriteOffset,
                    WriteLength,
                    out WrittenRangesUpdated);

            if (UpdatedRange.IsEmpty)
            {
                return QUIC_STATUS_OUT_OF_MEMORY;
            }
            if (!WrittenRangesUpdated)
            {
                return QUIC_STATUS_SUCCESS;
            }

            ReadyToRead = UpdatedRange.Low == 0;
            QuicRecvBufferCopyIntoChunks(RecvBuffer, WriteOffset, WriteLength, WriteBuffer);
            QuicRecvBufferValidate(RecvBuffer);
            return QUIC_STATUS_SUCCESS;
        }

        static void QuicRecvBufferValidate(QUIC_RECV_BUFFER RecvBuffer)
        {
            NetLog.Assert(
                (RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE &&
                RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED) ||
                RecvBuffer.RetiredChunk == null);

            NetLog.Assert(RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE || RecvBuffer.ReadStart == 0);
            NetLog.Assert(RecvBuffer.RetiredChunk == null || RecvBuffer.ReadPendingLength != 0);
            NetLog.Assert(RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED ||
                !CxPlatListIsEmpty(RecvBuffer.Chunks));

            if (CxPlatListIsEmpty(RecvBuffer.Chunks))
            {
                return;
            }

            QUIC_RECV_CHUNK FirstChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
            NetLog.Assert(
                (RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE &&
                RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_CIRCULAR) ||
                FirstChunk.Link.Next == RecvBuffer.Chunks);

            NetLog.Assert(
                (RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE &&
                RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED) ||
                RecvBuffer.ReadStart + RecvBuffer.ReadLength <= FirstChunk.AllocLength);
        }

        static void QuicRecvBufferCopyIntoChunks(QUIC_RECV_BUFFER RecvBuffer, int WriteOffset, int WriteLength, QUIC_SSBuffer WriteBuffer)
        {
            if (WriteOffset < RecvBuffer.BaseOffset)
            {
                //收到的数据有重叠了
                NetLog.Assert(RecvBuffer.BaseOffset - WriteOffset < ushort.MaxValue);
                int Diff = (RecvBuffer.BaseOffset - WriteOffset);
                WriteOffset += Diff;
                WriteLength -= Diff;
                WriteBuffer += Diff;
            }

            int RelativeOffset = WriteOffset - RecvBuffer.BaseOffset;
            QUIC_RECV_CHUNK_ITERATOR Iterator = QuicRecvBufferGetChunkIterator(RecvBuffer, RelativeOffset);
            QUIC_SSBuffer Buffer = QUIC_SSBuffer.Empty;
            while (WriteLength != 0 && QuicRecvChunkIteratorNext(ref Iterator, false, ref Buffer))
            {
                int CopyLength = Math.Min(Buffer.Length, WriteLength);
                WriteBuffer.Slice(0, CopyLength).CopyTo(Buffer);
                WriteBuffer += CopyLength;
                WriteLength -= CopyLength;
            }

            NetLog.Assert(WriteLength == 0); // Should always have enough room to copy everything
            QUIC_SUBRANGE FirstRange = QuicRangeGet(RecvBuffer.WrittenRanges, 0);
            if (FirstRange.Low == 0)
            {
                RecvBuffer.ReadLength = Math.Min(RecvBuffer.Capacity, FirstRange.Count - RecvBuffer.BaseOffset);
            }
        }

        static QUIC_RECV_CHUNK_ITERATOR QuicRecvBufferGetChunkIterator(QUIC_RECV_BUFFER RecvBuffer, int Offset)
        {
            QUIC_RECV_CHUNK_ITERATOR Iterator = new QUIC_RECV_CHUNK_ITERATOR();
            Iterator.NextChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
            Iterator.IteratorEnd = RecvBuffer.Chunks;

            if (Offset < RecvBuffer.Capacity)
            {
                Iterator.StartOffset = (RecvBuffer.ReadStart + Offset) % Iterator.NextChunk.AllocLength;
                Iterator.EndOffset = (RecvBuffer.ReadStart + RecvBuffer.Capacity - 1) % Iterator.NextChunk.AllocLength;
                return Iterator;
            }
                
            Offset -= RecvBuffer.Capacity;
            Iterator.NextChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Iterator.NextChunk.Link.Next);

            while (Offset >= Iterator.NextChunk.AllocLength)
            {
                NetLog.Assert(Iterator.NextChunk.Link.Next != RecvBuffer.Chunks);
                Offset -= Iterator.NextChunk.AllocLength;
                Iterator.NextChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Iterator.NextChunk.Link.Next);
            }

            Iterator.StartOffset = Offset;
            Iterator.EndOffset = Iterator.NextChunk.AllocLength - 1;
            return Iterator;
        }

        static bool QuicRecvChunkIteratorNext(ref QUIC_RECV_CHUNK_ITERATOR Iterator, bool ReferenceChunk, QUIC_BUFFER Buffer)
        {
            QUIC_SSBuffer mTempBuf = QUIC_SSBuffer.Empty;
            bool result = QuicRecvChunkIteratorNext(ref Iterator, ReferenceChunk, ref mTempBuf);
            Buffer.SetData(mTempBuf);
            return result;
        }

        static bool QuicRecvChunkIteratorNext(ref QUIC_RECV_CHUNK_ITERATOR Iterator, bool ReferenceChunk, ref QUIC_SSBuffer Buffer)
        {
            Buffer = QUIC_SSBuffer.Empty;
            if (Iterator.NextChunk == null)
            {
                return false;
            }

            if (ReferenceChunk)
            {
                Iterator.NextChunk.ExternalReference = true;
            }

            Buffer = Iterator.NextChunk.Buffer + Iterator.StartOffset;

            if (Iterator.StartOffset > Iterator.EndOffset)
            {
                Buffer.Length = Iterator.NextChunk.AllocLength - Iterator.StartOffset;
                Iterator.StartOffset = 0;
            }
            else
            {
                Buffer.Length = Iterator.EndOffset - Iterator.StartOffset + 1;
                if (Iterator.NextChunk.Link.Next == Iterator.IteratorEnd)
                {
                    Iterator.NextChunk = null;
                    return true;
                }

                Iterator.NextChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Iterator.NextChunk.Link.Next);
                Iterator.StartOffset = 0;
                Iterator.EndOffset = Iterator.NextChunk.AllocLength - 1;
            }
            return true;
        }

        static int QuicRecvBufferGetSpan(QUIC_RECV_BUFFER RecvBuffer)
        {
            return QuicRecvBufferGetTotalLength(RecvBuffer) - RecvBuffer.BaseOffset;
        }

        static int QuicRecvBufferGetTotalAllocLength(QUIC_RECV_BUFFER RecvBuffer)
        {
            QUIC_RECV_CHUNK Chunk = null;
            if (RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Prev);
                return Chunk.Buffer.Length;
            }

            Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
            if (Chunk.Link.Next == RecvBuffer.Chunks)
            {
                return Chunk.Buffer.Length;
            }

            int AllocLength = RecvBuffer.ReadLength;
            while (Chunk.Link.Next != RecvBuffer.Chunks)
            {
                Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Chunk.Link.Next);
                NetLog.Assert(AllocLength + Chunk.Buffer.Length < int.MaxValue);
                AllocLength += Chunk.Buffer.Length;
            }
            return AllocLength;
        }

        static bool QuicRecvBufferResize(QUIC_RECV_BUFFER RecvBuffer, int TargetBufferLength)
        {
            NetLog.Assert(RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED, "Should never resize in App-owned mode");
            NetLog.Assert(TargetBufferLength != 0 && (TargetBufferLength & (TargetBufferLength - 1)) == 0); // Power of 2
            NetLog.Assert(!CxPlatListIsEmpty(RecvBuffer.Chunks)); // Should always have at least one chunk
            QUIC_RECV_CHUNK LastChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Prev);
            NetLog.Assert(TargetBufferLength > LastChunk.AllocLength); // Should only be called when buffer needs to grow
            bool LastChunkIsFirst = LastChunk.Link.Prev == RecvBuffer.Chunks;

            QUIC_RECV_CHUNK NewChunk = new QUIC_RECV_CHUNK(TargetBufferLength);
            if (NewChunk == null)
            {
                return false;
            }

            QuicRecvChunkInitialize(NewChunk, TargetBufferLength, NewChunk.Buffer, false);
            CxPlatListInsertTail(RecvBuffer.Chunks, NewChunk.Link);

            if (!LastChunk.ExternalReference)
            {
                if (LastChunkIsFirst)
                {
                    int nSpan = QuicRecvBufferGetSpan(RecvBuffer);
                    if (nSpan < LastChunk.AllocLength)
                    {
                        nSpan = LastChunk.AllocLength;
                    }

                    int LengthTillWrap = LastChunk.AllocLength - RecvBuffer.ReadStart;
                    if (nSpan <= LengthTillWrap)
                    {
                        LastChunk.Buffer.Slice(RecvBuffer.ReadStart, nSpan).CopyTo(NewChunk.Buffer);
                    }
                    else
                    {
                        LastChunk.Buffer.Slice(RecvBuffer.ReadStart, LengthTillWrap).CopyTo(NewChunk.Buffer);
                        LastChunk.Buffer.Slice(0, nSpan - LengthTillWrap).CopyTo(NewChunk.Buffer + LengthTillWrap);
                    }
                    RecvBuffer.ReadStart = 0;
                    NetLog.Assert(NewChunk.AllocLength == TargetBufferLength);
                    RecvBuffer.Capacity = TargetBufferLength;
                }
                else
                {
                    LastChunk.Buffer.Slice(0, LastChunk.AllocLength).CopyTo(NewChunk.Buffer);
                }

                CxPlatListEntryRemove(LastChunk.Link);
                QuicRecvChunkFree(RecvBuffer, LastChunk);
                return true;
            }

            if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                return true;
            }

            int nSpanLength = QuicRecvBufferGetSpan(RecvBuffer);
            int nLengthTillWrap = LastChunk.AllocLength - RecvBuffer.ReadStart;
            if (nSpanLength <= nLengthTillWrap)
            {
                LastChunk.Buffer.Slice(RecvBuffer.ReadStart, nSpanLength).CopyTo(NewChunk.Buffer);
            }
            else
            {
                LastChunk.Buffer.Slice(RecvBuffer.ReadStart, nLengthTillWrap).CopyTo(NewChunk.Buffer);
                LastChunk.Buffer.Slice(0, nSpanLength - nLengthTillWrap).CopyTo(NewChunk.Buffer + nLengthTillWrap);
            }

            RecvBuffer.ReadStart = 0;
            RecvBuffer.Capacity = NewChunk.AllocLength;
            CxPlatListEntryRemove(LastChunk.Link);
            NetLog.Assert(RecvBuffer.RetiredChunk == null);
            RecvBuffer.RetiredChunk = LastChunk;

            return true;
        }

        static void QuicRecvBufferIncreaseVirtualBufferLength(QUIC_RECV_BUFFER RecvBuffer, int NewLength)
        {
            NetLog.Assert(NewLength >= RecvBuffer.VirtualBufferLength);
            RecvBuffer.VirtualBufferLength = NewLength;
        }

        
        static void QuicRecvChunkFree(QUIC_RECV_BUFFER RecvBuffer, QUIC_RECV_CHUNK Chunk)
        {
            if (Chunk == RecvBuffer.PreallocatedChunk)
            {
                return;
            }

            if (Chunk.AppOwnedBuffer)
            {
                Chunk.GetPool().CxPlatPoolFree(Chunk);
            }
            else
            {
                
            }
        }

    }
}

/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:18
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Diagnostics;

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
        
        public bool ExternalReference;
        public bool AppOwnedBuffer;  // Indicates the buffer is managed by the app
        public byte[] Buffer = null;

        public int AllocLength => Buffer != null ? Buffer.Length : 0;

        public QUIC_RECV_CHUNK(int nInitSize)
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_RECV_CHUNK>(this);
            Link = new CXPLAT_LIST_ENTRY<QUIC_RECV_CHUNK>(this);
            Buffer = new byte[nInitSize];
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
        public long ReadPendingLength;//当前已经向应用层“承诺”但尚未读完的总字节长度

        // 在这个偏移前面的 都是 好的有序的流，可以向应用程序分发数据
        // 在这个偏移后面的 所有后续乱序数据都以它为基准做相对偏移
        public long BaseOffset;
        public int ReadStart; //在环形物理缓存里的起始下标（index，不是逻辑偏移）。配合 ReadLength 指出“当前这次要拷贝给应用的那块连续内存”落在哪个片段。
        public int ReadLength; //本次准备拷贝给应用的连续字节数。

        //接收窗口大小（默认 512 KB，可动态增长）。
        //它决定 BaseOffset + VirtualBufferLength 这条“右沿”，任何逻辑偏移超过这条线的帧都会被临时拒绝（BUFFER_TOO_SMALL）
        public int VirtualBufferLength;
        public int Capacity; //当前已分配的物理内存总大小
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
            RecvBuffer.VirtualBufferLength = VirtualBufferLength;
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
            }
            else
            {
                RecvBuffer.Capacity = 0;
            }

            return QUIC_STATUS_SUCCESS;
        }

        static void QuicRecvChunkInitialize(QUIC_RECV_CHUNK Chunk, int AllocLength, byte[] Buffer, bool AppOwnedBuffer)
        {
            Chunk.Buffer = Buffer;
            Chunk.ExternalReference = false;
            Chunk.AppOwnedBuffer = AppOwnedBuffer;
        }

        static int QuicRecvBufferProvideChunks(QUIC_RECV_BUFFER RecvBuffer, CXPLAT_LIST_ENTRY Chunks)
        {
            NetLog.Assert(RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED);
            NetLog.Assert(!CxPlatListIsEmpty(Chunks));

            long NewBufferLength = QuicRecvBufferGetTotalAllocLength(RecvBuffer);
            for (CXPLAT_LIST_ENTRY Link = Chunks.Next; Link != Chunks; Link = Link.Next)
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
                QUIC_RECV_CHUNK FirstChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Chunks.Next);
                RecvBuffer.Capacity = FirstChunk.Buffer.Length;
            }

            RecvBuffer.VirtualBufferLength = Math.Max(RecvBuffer.VirtualBufferLength, (int)NewBufferLength);
            CxPlatListMoveItems(Chunks, RecvBuffer.Chunks);
            return QUIC_STATUS_SUCCESS;
        }

        static void QuicRecvBufferUninitialize(QUIC_RECV_BUFFER RecvBuffer)
        {
            QuicRangeUninitialize(RecvBuffer.WrittenRanges);
            while (!CxPlatListIsEmpty(RecvBuffer.Chunks))
            {
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(CxPlatListRemoveHead(RecvBuffer.Chunks));
                QuicRecvChunkFree(RecvBuffer, Chunk);
            }

            if (RecvBuffer.RetiredChunk != null)
            {
                QuicRecvChunkFree(RecvBuffer, RecvBuffer.RetiredChunk);
            }
        }

        static long QuicRecvBufferGetTotalLength(QUIC_RECV_BUFFER RecvBuffer)
        {
            if (QuicRangeGetMaxSafe(RecvBuffer.WrittenRanges, out ulong TotalLength))
            {
                TotalLength++;
            }
            NetLog.Assert((long)TotalLength >= RecvBuffer.BaseOffset);
            return (long)TotalLength;
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
            if (FirstRange == null || FirstRange.Low != 0)
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
            NetLog.Assert(QuicRangeGetSafe(RecvBuffer.WrittenRanges, 0) != null);
            NetLog.Assert(!CxPlatListIsEmpty(RecvBuffer.Chunks));
            NetLog.Assert(RecvBuffer.ReadPendingLength == 0 || RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE);

            QUIC_SUBRANGE FirstRange = QuicRangeGet(RecvBuffer.WrittenRanges, 0);
            NetLog.Assert(FirstRange.Low == 0 || (int)FirstRange.Count > RecvBuffer.BaseOffset);
            long ContiguousLength = FirstRange.Count - RecvBuffer.BaseOffset;

            QUIC_RECV_CHUNK_ITERATOR Iterator = QuicRecvBufferGetChunkIterator(RecvBuffer, RecvBuffer.ReadPendingLength);
            long ReadableDataLeft = ContiguousLength - RecvBuffer.ReadPendingLength;
            int CurrentBufferId = 0;

            while (CurrentBufferId < BufferCount && ReadableDataLeft > 0 && 
                QuicRecvChunkIteratorNext(ref Iterator, true, Buffers[CurrentBufferId]))
            {
                if (Buffers[CurrentBufferId].Length > ReadableDataLeft)
                {
                    Buffers[CurrentBufferId].Length = (int)ReadableDataLeft;
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

        //把已经连续收齐、且应用也已读完的物理内存块归还给池/系统” 的辅助函数，核心目的只有一句话：
        //接收侧已经攒够一段连续且被应用消耗掉的数据，就把底下对应的物理 chunk 释放掉，省内存。
        static void QuicRecvBufferDrainFullChunks(QUIC_RECV_BUFFER RecvBuffer, ref long DrainLength)
        {
            long RemainingDrainLength = DrainLength;
            QUIC_RECV_CHUNK_ITERATOR Iterator = QuicRecvBufferGetChunkIterator(RecvBuffer, 0);
            QUIC_RECV_CHUNK NewFirstChunk = Iterator.NextChunk;
            while (QuicRecvChunkIteratorNext(ref Iterator, false, out QUIC_SSBuffer Buffer))
            {
                if (RemainingDrainLength < Buffer.Length)
                {
                    break;
                }

                RemainingDrainLength -= Buffer.Length;
                NewFirstChunk = Iterator.NextChunk;
            }

            if (NewFirstChunk != null && NewFirstChunk.Link == RecvBuffer.Chunks.Next)
            {
                return;
            }

            NetLog.Assert(RemainingDrainLength == 0 || NewFirstChunk != null);
            if (NewFirstChunk == null && RecvBuffer.RecvMode !=  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED)
            {
                NewFirstChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Prev);
                NewFirstChunk.ExternalReference = false;
            }
            
            CXPLAT_LIST_ENTRY ChunkIt = RecvBuffer.Chunks.Next;
            CXPLAT_LIST_ENTRY EndIt = NewFirstChunk != null ? NewFirstChunk.Link : RecvBuffer.Chunks;
            while (ChunkIt != EndIt)
            {
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(ChunkIt);
                ChunkIt = ChunkIt.Next;

                CxPlatListEntryRemove(Chunk.Link);
                QuicRecvChunkFree(RecvBuffer, Chunk);
            }

            RecvBuffer.Capacity = NewFirstChunk != null ? NewFirstChunk.AllocLength : 0;
            RecvBuffer.ReadStart = 0;
            RecvBuffer.ReadLength = Math.Min(RecvBuffer.Capacity,
                   (int)(QuicRangeGet(RecvBuffer.WrittenRanges, 0).Count - RecvBuffer.BaseOffset));

            DrainLength = RemainingDrainLength;
        }


        static void QuicRecvBufferDrainFirstChunk(QUIC_RECV_BUFFER RecvBuffer, long DrainLength)
        {
            QUIC_RECV_CHUNK FirstChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
            NetLog.Assert(DrainLength < RecvBuffer.Capacity);

            RecvBuffer.ReadStart = (int)(RecvBuffer.ReadStart + DrainLength) % FirstChunk.AllocLength;

            if (RecvBuffer.RecvMode ==  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED ||
                FirstChunk.Link.Next != RecvBuffer.Chunks)
            {
                RecvBuffer.Capacity -= (int)DrainLength;
            }

            RecvBuffer.ReadLength = Math.Min(RecvBuffer.Capacity,
                   (int)(QuicRangeGet(RecvBuffer.WrittenRanges, 0).Count - RecvBuffer.BaseOffset));

            if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE && RecvBuffer.ReadStart != 0)
            {
                FirstChunk.Buffer.AsSpan().Slice(RecvBuffer.ReadStart, FirstChunk.AllocLength - RecvBuffer.ReadStart).CopyTo(FirstChunk.Buffer);
                RecvBuffer.ReadStart = 0;
            }
        }
        
        //用于手动清空 接收缓冲区中 已被应用程序读取的字节数量。
        static bool QuicRecvBufferDrain(QUIC_RECV_BUFFER RecvBuffer, long DrainLength)
        {
            NetLog.Assert(QuicRangeGetSafe(RecvBuffer.WrittenRanges, 0) != null);
            NetLog.Assert(DrainLength <= RecvBuffer.ReadPendingLength);
            NetLog.Assert(!CxPlatListIsEmpty(RecvBuffer.Chunks));
            NetLog.Assert(DrainLength <= RecvBuffer.VirtualBufferLength);

            if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                RecvBuffer.ReadPendingLength -= DrainLength;
            }
            else
            {
                RecvBuffer.ReadPendingLength = 0;
            }

            RecvBuffer.BaseOffset += DrainLength;
            if (RecvBuffer.RetiredChunk != null)
            {
                NetLog.Assert(RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE ||
                    RecvBuffer.RecvMode ==  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_CIRCULAR);

                QuicRecvChunkFree(RecvBuffer, RecvBuffer.RetiredChunk);
                RecvBuffer.RetiredChunk = null;
            }

            QuicRecvBufferDrainFullChunks(RecvBuffer, ref DrainLength);
            if (CxPlatListIsEmpty(RecvBuffer.Chunks))
            {
                NetLog.Assert(RecvBuffer.RecvMode ==  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED);
                NetLog.Assert(DrainLength == 0);
                return true;
            }

            QuicRecvBufferDrainFirstChunk(RecvBuffer, DrainLength);
            if (RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                for (CXPLAT_LIST_ENTRY Link = RecvBuffer.Chunks.Next; Link != RecvBuffer.Chunks; Link = Link.Next)
                {
                    QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Link);
                    Chunk.ExternalReference = false;
                }
            }
            else
            {
                QUIC_RECV_CHUNK FirstChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
                FirstChunk.ExternalReference = RecvBuffer.ReadPendingLength != 0;
            }

            QuicRecvBufferValidate(RecvBuffer);
            return RecvBuffer.ReadLength == 0;
        }

        //看看现在能不能再塞一段新数据给应用” 的快捷判断——只有当连续收到的新字节比应用尚未读完的字节多时
        static bool QuicRecvBufferHasUnreadData(QUIC_RECV_BUFFER RecvBuffer)
        {
            QUIC_SUBRANGE FirstRange = QuicRangeGetSafe(RecvBuffer.WrittenRanges, 0);
            if (FirstRange == null || FirstRange.Low != 0)
            {
                return false;
            }

            NetLog.Assert((int)FirstRange.Count >= RecvBuffer.BaseOffset);
            long ContiguousLength = FirstRange.Count - RecvBuffer.BaseOffset;
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

        //Quota限额
        static int QuicRecvBufferWrite(QUIC_RECV_BUFFER RecvBuffer, long WriteOffset, ushort WriteLength, 
            QUIC_SSBuffer WriteBuffer, long WriteQuota, 
            out long QuotaConsumed, out bool NewDataReady, out long BufferSizeNeeded)
        {
            NetLog.Assert(WriteLength != 0);
            NewDataReady = false;
            QuotaConsumed = 0;
            BufferSizeNeeded = 0;

            long AbsoluteLength = WriteOffset + WriteLength;
            if (AbsoluteLength <= RecvBuffer.BaseOffset)
            {
                //RecvBuffer->BaseOffset 是接收端已按序交付给应用的最远位置（也就是“缺口”的起始位置）。
                //如果本次数据整个区间都在 BaseOffset 左边（含等于），
                //说明它早已收到并交付过，现在又来一份完全重叠或更老的副本，自然可以直接忽略
                return QUIC_STATUS_SUCCESS;
            }
            
            if (AbsoluteLength > RecvBuffer.BaseOffset + RecvBuffer.VirtualBufferLength)
            {
                //是接收端为这条流预分配的“虚拟环形缓冲区”总大小（默认 512 KB，可随窗口自动增长，但增长前是定值）。
                //它表示：在当前 BaseOffset 之后，我最多还能缓存多少字节的乱序数据。
                //RecvBuffer.BaseOffset + RecvBuffer.VirtualBufferLength
                //就是“当前我能接受的最远逻辑偏移”。任何数据只要右边界超过这条线，就暂时没地方存。
                //一旦本次 STREAM 帧的 WriteOffset+WriteLength 大于上面那条线，
                //说明它完全或部分落在缓冲区之外，即使后来数据真的有用，现在也没空间缓存，于是直接返回
                //QUIC_STATUS_BUFFER_TOO_SMALL
                //告诉发送方：“我这边窗口 / 缓存不够，你先别发”
                return QUIC_STATUS_BUFFER_TOO_SMALL;
            }
            
            long CurrentMaxLength = QuicRecvBufferGetTotalLength(RecvBuffer);
            if (AbsoluteLength > CurrentMaxLength)
            {
                //CurrentMaxLength 返回的是当前已缓存的乱序数据总长度（单位：字节）。
                //只有本次 STREAM 帧要写入的右边界超过了当前已缓存的尾部，才会真正新增缓存占用。
                //否则只是“填洞”，不会多占内存，直接放行。
                // WriteQuota 是连接级的“单次写入配额”，默认 64 KB（可配）。
                //如果本次新增占用比剩余配额还大，就拒绝写入，返回
                //QUIC_STATUS_BUFFER_TOO_SMALL
                //相当于说：“我不能再让你一次性囤这么多乱序数据，先缓一缓。”
                //如果配额够用，则把本次实际新增占用记下来，后面统一扣减连接级总配额
                if (AbsoluteLength - CurrentMaxLength > WriteQuota)
                {
                    return QUIC_STATUS_BUFFER_TOO_SMALL;
                }
                QuotaConsumed = AbsoluteLength - CurrentMaxLength;
            }

            //物理内存按需倍增
            int AllocLength = QuicRecvBufferGetTotalAllocLength(RecvBuffer);
            if (AbsoluteLength > RecvBuffer.BaseOffset + AllocLength)
            {
                if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED)
                {
                    BufferSizeNeeded = AbsoluteLength - (RecvBuffer.BaseOffset + AllocLength);
                    return QUIC_STATUS_BUFFER_TOO_SMALL;
                }

                QUIC_RECV_CHUNK LastChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Prev);
                int NewBufferLength = LastChunk.AllocLength << 1;
                while (AbsoluteLength > RecvBuffer.BaseOffset + NewBufferLength)
                {
                    NewBufferLength <<= 1;
                }

                if (!QuicRecvBufferResize(RecvBuffer, NewBufferLength))
                {
                    BufferSizeNeeded = AbsoluteLength - (RecvBuffer.BaseOffset + AllocLength);
                    return QUIC_STATUS_OUT_OF_MEMORY;
                }
            }

            bool WrittenRangesUpdated = false;
            QUIC_SUBRANGE UpdatedRange = QuicRangeAddRange(
                    RecvBuffer.WrittenRanges,
                    (ulong)WriteOffset,
                    WriteLength,
                    out WrittenRangesUpdated);

            if (UpdatedRange == null)
            {
                return QUIC_STATUS_OUT_OF_MEMORY;
            }

            if (!WrittenRangesUpdated)
            {
                return QUIC_STATUS_SUCCESS;
            }

            NewDataReady = UpdatedRange.Low == 0;
            QuicRecvBufferCopyIntoChunks(RecvBuffer, WriteOffset, WriteLength, WriteBuffer);
            QuicRecvBufferValidate(RecvBuffer);
            return QUIC_STATUS_SUCCESS;
        }

        [Conditional("DEBUG")]
        static void QuicRecvBufferValidate(QUIC_RECV_BUFFER RecvBuffer)
        {
#if DEBUG
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
#endif
        }

        static void QuicRecvBufferCopyIntoChunks(QUIC_RECV_BUFFER RecvBuffer, long WriteOffset, ushort WriteLength, QUIC_SSBuffer WriteBuffer)
        {
            if (WriteOffset < RecvBuffer.BaseOffset)
            {
                //收到的数据有重叠了
                NetLog.Assert(RecvBuffer.BaseOffset - WriteOffset < ushort.MaxValue);
                ushort Diff = (ushort)(RecvBuffer.BaseOffset - WriteOffset);
                WriteOffset += Diff;
                WriteLength -= Diff;
                WriteBuffer += Diff;
            }

            long RelativeOffset = WriteOffset - RecvBuffer.BaseOffset;
            QUIC_RECV_CHUNK_ITERATOR Iterator = QuicRecvBufferGetChunkIterator(RecvBuffer, RelativeOffset);
            while (WriteLength != 0 && QuicRecvChunkIteratorNext(ref Iterator, false, out QUIC_SSBuffer Buffer))
            {
                int CopyLength = Math.Min(Buffer.Length, WriteLength);
                WriteBuffer.Slice(0, CopyLength).CopyTo(Buffer);
                WriteBuffer += CopyLength;
                WriteLength -= (ushort)CopyLength;
            }

            NetLog.Assert(WriteLength == 0); // Should always have enough room to copy everything
            QUIC_SUBRANGE FirstRange = QuicRangeGet(RecvBuffer.WrittenRanges, 0);
            if (FirstRange.Low == 0)
            {
                RecvBuffer.ReadLength = (int)Math.Min(RecvBuffer.Capacity, FirstRange.Count - RecvBuffer.BaseOffset);
            }
        }

        static QUIC_RECV_CHUNK_ITERATOR QuicRecvBufferGetChunkIterator(QUIC_RECV_BUFFER RecvBuffer, long Offset)
        {
            QUIC_RECV_CHUNK_ITERATOR Iterator = new QUIC_RECV_CHUNK_ITERATOR();
            Iterator.NextChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
            Iterator.IteratorEnd = RecvBuffer.Chunks;

            if (Offset < RecvBuffer.Capacity)
            {
                Iterator.StartOffset = (int)(RecvBuffer.ReadStart + Offset) % Iterator.NextChunk.AllocLength;
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

            Iterator.StartOffset = (int)Offset;
            Iterator.EndOffset = Iterator.NextChunk.AllocLength - 1;
            return Iterator;
        }

        static bool QuicRecvChunkIteratorNext(ref QUIC_RECV_CHUNK_ITERATOR Iterator, bool ReferenceChunk, QUIC_BUFFER Buffer)
        {
            bool result = QuicRecvChunkIteratorNext(ref Iterator, ReferenceChunk, out QUIC_SSBuffer mTempBuf);
            Buffer.SetData(mTempBuf);
            return result;
        }

        static bool QuicRecvChunkIteratorNext(ref QUIC_RECV_CHUNK_ITERATOR Iterator, bool ReferenceChunk, out QUIC_SSBuffer Buffer)
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

            Buffer = Iterator.NextChunk.Buffer;
            Buffer += Iterator.StartOffset;

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
            return (int)(QuicRecvBufferGetTotalLength(RecvBuffer) - RecvBuffer.BaseOffset);
        }

        static int QuicRecvBufferGetTotalAllocLength(QUIC_RECV_BUFFER RecvBuffer)
        {
            if (CxPlatListIsEmpty(RecvBuffer.Chunks))
            {
                return 0;
            }

            int AllocLength = RecvBuffer.Capacity;
            //这里忽略第一个块
            // 如果存在更多区块且该区块正在被使用，则第一个区块的容量可能会减少
            // 已被消耗）。其他块始终以其完整分配大小进行分配。
            for (CXPLAT_LIST_ENTRY Link = RecvBuffer.Chunks.Next.Next; Link != RecvBuffer.Chunks; Link = Link.Next)
            {
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Link);
                AllocLength += Chunk.AllocLength;
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
                        LastChunk.Buffer.AsSpan().Slice(RecvBuffer.ReadStart, nSpan).CopyTo(NewChunk.Buffer);
                    }
                    else
                    {
                        LastChunk.Buffer.AsSpan().Slice(RecvBuffer.ReadStart, LengthTillWrap).CopyTo(NewChunk.Buffer);
                        LastChunk.Buffer.AsSpan().Slice(0, nSpan - LengthTillWrap).CopyTo(NewChunk.Buffer.AsSpan().Slice(LengthTillWrap));
                    }

                    RecvBuffer.ReadStart = 0;
                    NetLog.Assert(NewChunk.AllocLength == TargetBufferLength);
                    RecvBuffer.Capacity = TargetBufferLength;
                }
                else
                {
                    LastChunk.Buffer.AsSpan().CopyTo(NewChunk.Buffer);
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
                LastChunk.Buffer.AsSpan().Slice(RecvBuffer.ReadStart, nSpanLength).CopyTo(NewChunk.Buffer);
            }
            else
            {
                LastChunk.Buffer.AsSpan().Slice(RecvBuffer.ReadStart, nLengthTillWrap).CopyTo(NewChunk.Buffer);
                LastChunk.Buffer.AsSpan().Slice(0, nSpanLength - nLengthTillWrap).CopyTo(NewChunk.Buffer.AsSpan().Slice(nLengthTillWrap));
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

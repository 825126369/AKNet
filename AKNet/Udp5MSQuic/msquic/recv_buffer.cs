﻿using AKNet.Common;
using AKNet.Udp5MSQuic.Common;
using System;

namespace AKNet.Udp5MSQuic.Common
{
    internal enum QUIC_RECV_BUF_MODE
    {
        QUIC_RECV_BUF_MODE_SINGLE,      // Only one receive with a single contiguous buffer at a time.
        QUIC_RECV_BUF_MODE_CIRCULAR,    // Only one receive that may indicate two contiguous buffers at a time.
        QUIC_RECV_BUF_MODE_MULTIPLE,    // Multiple independent receives that may indicate up to two contiguous buffers at a time.
        QUIC_RECV_BUF_MODE_APP_OWNED    // Uses memory buffers provided by the app. Only one receive at a time,
    }

    internal class QUIC_RECV_CHUNK : QUIC_BUFFER, CXPLAT_POOL_Interface<QUIC_RECV_CHUNK>
    {
        public readonly CXPLAT_LIST_ENTRY<QUIC_RECV_CHUNK> Link;
        public bool ExternalReference;
        
        public readonly CXPLAT_POOL_ENTRY<QUIC_RECV_CHUNK> POOL_ENTRY = null;
        public QUIC_RECV_CHUNK(int nInitSize)
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_RECV_CHUNK>(this);
            Link = new CXPLAT_LIST_ENTRY<QUIC_RECV_CHUNK>(this);

            Buffer = new byte[nInitSize];
            Offset = 0;
            Length = nInitSize;
        }

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
        public readonly CXPLAT_LIST_ENTRY Chunks = new CXPLAT_LIST_ENTRY<QUIC_RECV_CHUNK>(null);
        public QUIC_RECV_CHUNK PreallocatedChunk;
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
        static ulong QuicRecvBufferInitialize(QUIC_RECV_BUFFER RecvBuffer, int AllocBufferLength,
            int VirtualBufferLength, QUIC_RECV_BUF_MODE RecvMode,
            CXPLAT_POOL<QUIC_RECV_CHUNK> AppBufferChunkPool, QUIC_RECV_CHUNK PreallocatedChunk)
        {
            ulong Status;
            NetLog.Assert(AllocBufferLength != 0 || RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED);
            NetLog.Assert(VirtualBufferLength != 0 || RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_APP_OWNED);
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
                Chunk = new QUIC_RECV_CHUNK(AllocBufferLength);
                if (Chunk == null)
                {
                    Status = QUIC_STATUS_OUT_OF_MEMORY;
                    goto Error;
                }
            }

            QuicRangeInitialize(QUIC_MAX_RANGE_ALLOC_SIZE, RecvBuffer.WrittenRanges);
            CxPlatListInitializeHead(RecvBuffer.Chunks);
            CxPlatListInsertHead(RecvBuffer.Chunks, Chunk.Link);
            Chunk.Length = AllocBufferLength;
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

        static ulong QuicRecvBufferProvideChunks(QUIC_RECV_BUFFER RecvBuffer, CXPLAT_LIST_ENTRY Chunks)
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

        static void QuicRecvBufferRead(QUIC_RECV_BUFFER RecvBuffer, ref int BufferOffset, ref int BufferCount, QUIC_BUFFER[] Buffers)
        {
            NetLog.Assert(!QuicRangeGetSafe(RecvBuffer.WrittenRanges, 0).IsEmpty);
            NetLog.Assert(!CxPlatListIsEmpty(RecvBuffer.Chunks));
            NetLog.Assert(RecvBuffer.ReadPendingLength == 0 || RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE);
            NetLog.Assert(RecvBuffer.Chunks.Next.Next == RecvBuffer.Chunks || RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE);

            QUIC_SUBRANGE FirstRange = QuicRangeGet(RecvBuffer.WrittenRanges, 0);
            NetLog.Assert(FirstRange.Low == 0 || (int)FirstRange.Count > RecvBuffer.BaseOffset);
            int ContiguousLength = (int)FirstRange.Count - RecvBuffer.BaseOffset;

            if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE)
            {
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
                NetLog.Assert(!Chunk.ExternalReference);
                NetLog.Assert(RecvBuffer.ReadStart == 0);
                NetLog.Assert(BufferCount >= 1);
                NetLog.Assert(ContiguousLength <= Chunk.Length);

                BufferCount = 1;
                BufferOffset = RecvBuffer.BaseOffset;
                RecvBuffer.ReadPendingLength += ContiguousLength;
                Buffers[0].Length = ContiguousLength;
                Buffers[0].Buffer = Chunk.Buffer;
                Chunk.ExternalReference = true;
            }
            else if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_CIRCULAR)
            {
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
                NetLog.Assert(!Chunk.ExternalReference);
                NetLog.Assert(BufferCount >= 2);
                NetLog.Assert(ContiguousLength <= Chunk.Length);

                BufferOffset = RecvBuffer.BaseOffset;
                RecvBuffer.ReadPendingLength += ContiguousLength;
                Chunk.ExternalReference = true;

                int ReadStart = RecvBuffer.ReadStart;
                if (ReadStart + ContiguousLength > Chunk.Length)
                {
                    BufferCount = 2;
                    Buffers[0].Buffer = Chunk.Buffer;
                    Buffers[0].Length = Chunk.Length - ReadStart;
                    Buffers[0].Offset = ReadStart;

                    Buffers[1].Buffer = Chunk.Buffer;
                    Buffers[1].Length = ContiguousLength - Buffers[0].Length;
                    Buffers[1].Offset = 0;
                }
                else
                {
                    BufferCount = 1;
                    Buffers[0].Buffer = Chunk.Buffer;
                    Buffers[0].Length = ContiguousLength;
                    Buffers[0].Offset = ReadStart;
                }
            }
            else
            {
                NetLog.Assert(RecvBuffer.ReadPendingLength < ContiguousLength); // Shouldn't call read if there is nothing new to read
                int UnreadLength = ContiguousLength - RecvBuffer.ReadPendingLength;
                NetLog.Assert(UnreadLength > 0);

                int ChunkReadOffset = RecvBuffer.ReadPendingLength;
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
                bool IsFirstChunk = true;
                int ChunkReadLength = RecvBuffer.ReadLength;
                while (ChunkReadLength <= ChunkReadOffset)
                {
                    NetLog.Assert(ChunkReadLength != 0);
                    NetLog.Assert(Chunk.ExternalReference);
                    NetLog.Assert(Chunk.Link.Next != RecvBuffer.Chunks);
                    ChunkReadOffset -= ChunkReadLength;
                    IsFirstChunk = false;
                    Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Chunk.Link.Next);
                    ChunkReadLength = Chunk.Length;
                }
                NetLog.Assert(BufferCount >= 3);
                NetLog.Assert(ChunkReadOffset <= int.MaxValue);

                ChunkReadLength -= ChunkReadOffset;
                if (IsFirstChunk)
                {
                    ChunkReadOffset = (RecvBuffer.ReadStart + ChunkReadOffset) % Chunk.Length;
                    NetLog.Assert(ChunkReadLength <= UnreadLength);
                }
                else if (ChunkReadLength > UnreadLength)
                {
                    ChunkReadLength = UnreadLength;
                }

                NetLog.Assert(ChunkReadLength <= Chunk.Length);
                if (ChunkReadOffset + ChunkReadLength > Chunk.Buffer.Length)
                {
                    BufferCount = 2;
                    Buffers[0].Offset = ChunkReadOffset;
                    Buffers[0].Length = Chunk.Length - ChunkReadOffset;
                    Buffers[0].Buffer = Chunk.Buffer;
                    Buffers[1].Offset = 0;
                    Buffers[1].Length = ChunkReadLength - Buffers[0].Length;
                    Buffers[1].Buffer = Chunk.Buffer;
                }
                else
                {
                    BufferCount = 1;
                    Buffers[0].Offset = ChunkReadOffset;
                    Buffers[0].Length = ChunkReadLength;
                    Buffers[0].Buffer = Chunk.Buffer;
                }

                Chunk.ExternalReference = true;
                if (UnreadLength > ChunkReadLength)
                {
                    NetLog.Assert(Chunk.Link.Next != RecvBuffer.Chunks);
                    ChunkReadLength = UnreadLength - ChunkReadLength;
                    Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Chunk.Link.Next);
                    NetLog.Assert(ChunkReadLength <= Chunk.Buffer.Length);
                    Buffers[BufferCount].Length = ChunkReadLength;
                    Buffers[BufferCount].Buffer = Chunk.Buffer;
                    BufferCount = BufferCount + 1;
                    Chunk.ExternalReference = true;
                }

                BufferOffset = RecvBuffer.BaseOffset + RecvBuffer.ReadPendingLength;
                RecvBuffer.ReadPendingLength += UnreadLength;
            }
        }

        static bool QuicRecvBufferDrain(QUIC_RECV_BUFFER RecvBuffer, int DrainLength)
        {
            NetLog.Assert(DrainLength <= RecvBuffer.ReadPendingLength);
            if (RecvBuffer.RecvMode !=  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
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
                    for (int i = 0; i < Chunk.Length - DrainLength; i++)
                    {
                        Chunk.Buffer[i] = Chunk.Buffer[i + DrainLength];
                    }
                }
                else
                {
                    RecvBuffer.ReadStart = ((RecvBuffer.ReadStart + DrainLength) % Chunk.Length);
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
            NetLog.Assert(RecvBuffer.RecvMode ==  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_SINGLE);
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
            if (RecvBuffer.RecvMode ==  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
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

            if (RecvBuffer.RecvMode ==  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);
                RecvBuffer.Capacity = Chunk.Length;
                if (Chunk.Length < RecvBuffer.ReadLength)
                {
                    RecvBuffer.ReadLength = Chunk.Length;
                }
            }

            return DrainLength;
        }

        static ulong QuicRecvBufferWrite(QUIC_RECV_BUFFER RecvBuffer, QUIC_SSBuffer WriteBuffer, int WriteLimit, ref bool ReadyToRead)
        {
            NetLog.Assert(WriteBuffer.Length != 0);
            ReadyToRead = false; // Most cases below aren't ready to read.
            int AbsoluteLength = WriteBuffer.Offset + WriteBuffer.Length;
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

            int AllocLength = QuicRecvBufferGetTotalAllocLength(RecvBuffer);
            if (AbsoluteLength > RecvBuffer.BaseOffset + AllocLength)
            {
                int NewBufferLength = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Prev).Length << 1;
                while (AbsoluteLength > RecvBuffer.BaseOffset + NewBufferLength + RecvBuffer.ReadPendingLength)
                {
                    NewBufferLength <<= 1;
                }
                if (!QuicRecvBufferResize(RecvBuffer, NewBufferLength))
                {
                    return QUIC_STATUS_OUT_OF_MEMORY;
                }
            }

            bool WrittenRangesUpdated = false;
            QUIC_SUBRANGE UpdatedRange = QuicRangeAddRange(
                    RecvBuffer.WrittenRanges,
                    (ulong)WriteBuffer.Offset,
                    WriteBuffer.Length,
                    ref WrittenRangesUpdated);

            if (UpdatedRange.IsEmpty)
            {
                return QUIC_STATUS_OUT_OF_MEMORY;
            }
            if (!WrittenRangesUpdated)
            {
                return QUIC_STATUS_SUCCESS;
            }

            ReadyToRead = UpdatedRange.Low == 0;
            QuicRecvBufferCopyIntoChunks(RecvBuffer, WriteBuffer);
            return QUIC_STATUS_SUCCESS;
        }

        static void QuicRecvBufferCopyIntoChunks(QUIC_RECV_BUFFER RecvBuffer, QUIC_SSBuffer WriteBuffer)
        {
            if (WriteBuffer.Offset < RecvBuffer.BaseOffset)
            {
                NetLog.Assert(RecvBuffer.BaseOffset - WriteBuffer.Offset < ushort.MaxValue);
                int Diff = (RecvBuffer.BaseOffset - WriteBuffer.Offset);
                WriteBuffer.Offset += Diff;
                WriteBuffer.Length -= Diff;
            }

            if (RecvBuffer.RecvMode != QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Prev); // Last chunk
                NetLog.Assert(WriteBuffer.Length <= Chunk.Buffer.Length);
                int RelativeOffset = WriteBuffer.Offset - RecvBuffer.BaseOffset;
                int ChunkOffset = (RecvBuffer.ReadStart + RelativeOffset) % Chunk.Buffer.Length;

                if (ChunkOffset + WriteBuffer.Length > Chunk.Buffer.Length)
                {
                    int Part1Len = Chunk.Buffer.Length - ChunkOffset;
                    Array.Copy(WriteBuffer.Buffer, WriteBuffer.Offset, Chunk.Buffer, ChunkOffset, Part1Len);
                    Array.Copy(WriteBuffer.Buffer, WriteBuffer.Offset + Part1Len, Chunk.Buffer, 0, WriteBuffer.Length - Part1Len);
                }
                else
                {
                    Array.Copy(Chunk.Buffer, ChunkOffset, WriteBuffer.Buffer, WriteBuffer.Offset, WriteBuffer.Length);
                }

                if (Chunk.Link.Next == RecvBuffer.Chunks)
                {
                    RecvBuffer.ReadLength = QuicRangeGet(RecvBuffer.WrittenRanges, 0).Count - RecvBuffer.BaseOffset;
                }
            }
            else
            {
                QUIC_RECV_CHUNK Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Next);

                int ChunkLength;
                bool IsFirstChunk = true;
                int RelativeOffset = WriteBuffer.Offset - RecvBuffer.BaseOffset;
                int ChunkOffset = RecvBuffer.ReadStart;
                if (Chunk.Link.Next == RecvBuffer.Chunks)
                {
                    NetLog.Assert(WriteBuffer.Length <= Chunk.Length); // Should always fit if we only have one
                    ChunkLength = Chunk.Length;
                    RecvBuffer.ReadLength = QuicRangeGet(RecvBuffer.WrittenRanges, 0).Count - RecvBuffer.BaseOffset;
                }
                else
                {
                    ChunkLength = RecvBuffer.Capacity;

                    if (RelativeOffset < RecvBuffer.Capacity)
                    {
                        RecvBuffer.ReadLength = QuicRangeGet(RecvBuffer.WrittenRanges, 0).Count - RecvBuffer.BaseOffset;
                        if (RecvBuffer.Capacity < RecvBuffer.ReadLength)
                        {
                            RecvBuffer.ReadLength = RecvBuffer.Capacity;
                        }
                    }
                    else
                    {
                        while (ChunkLength <= RelativeOffset)
                        {
                            RelativeOffset -= ChunkLength;
                            IsFirstChunk = false;
                            Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Chunk.Link.Next);
                            ChunkLength = Chunk.Buffer.Length;
                        }
                    }
                }

                bool IsFirstLoop = true;
                do
                {
                    int ChunkWriteOffset = (ChunkOffset + RelativeOffset) % Chunk.Buffer.Length;
                    if (!IsFirstChunk)
                    {
                        ChunkWriteOffset = RelativeOffset;
                    }
                    if (!IsFirstLoop)
                    {
                        ChunkWriteOffset = 0;
                    }

                    int ChunkWriteLength = WriteBuffer.Length;
                    if (IsFirstChunk)
                    {
                        if (RecvBuffer.Capacity < RelativeOffset + ChunkWriteLength)
                        {
                            ChunkWriteLength = RecvBuffer.Capacity - RelativeOffset;
                        }
                        if (Chunk.Buffer.Length < ChunkWriteOffset + ChunkWriteLength)
                        {
                            Array.Copy(WriteBuffer.Buffer, 0, Chunk.Buffer, ChunkWriteOffset, Chunk.Length - ChunkWriteOffset);
                            Array.Copy(WriteBuffer.Buffer, Chunk.Buffer.Length - ChunkWriteOffset, Chunk.Buffer, 0, ChunkWriteLength - (Chunk.Length - ChunkWriteOffset));
                        }
                        else
                        {
                            Array.Copy(WriteBuffer.Buffer, 0, Chunk.Buffer, Chunk.Offset + ChunkWriteOffset, ChunkWriteLength);
                        }
                    }
                    else
                    {
                        if (ChunkWriteOffset + ChunkWriteLength >= ChunkLength)
                        {
                            ChunkWriteLength = ChunkLength - ChunkWriteOffset;
                        }
                        Array.Copy(WriteBuffer.Buffer, 0, Chunk.Buffer, Chunk.Offset + ChunkWriteOffset, ChunkWriteLength);
                    }

                    if (WriteBuffer.Length == ChunkWriteLength)
                    {
                        break;
                    }

                    WriteBuffer.Offset += ChunkWriteLength;
                    WriteBuffer.Length -= ChunkWriteLength;
                    Chunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(Chunk.Link.Next);
                    ChunkOffset = 0;
                    ChunkLength = Chunk.Buffer.Length;
                    IsFirstChunk = false;
                    IsFirstLoop = false;

                } while (true);
            }
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
            NetLog.Assert(TargetBufferLength != 0 && (TargetBufferLength & (TargetBufferLength - 1)) == 0); // Power of 2
            NetLog.Assert(!CxPlatListIsEmpty(RecvBuffer.Chunks)); // Should always have at least one chunk
            QUIC_RECV_CHUNK LastChunk = CXPLAT_CONTAINING_RECORD<QUIC_RECV_CHUNK>(RecvBuffer.Chunks.Prev);
            NetLog.Assert(TargetBufferLength > LastChunk.Buffer.Length); // Should only be called when buffer needs to grow
            bool LastChunkIsFirst = LastChunk.Link.Prev == RecvBuffer.Chunks;

            QUIC_RECV_CHUNK NewChunk = new QUIC_RECV_CHUNK(TargetBufferLength);
            if (NewChunk == null)
            {
                return false;
            }

            NewChunk.ExternalReference = false;
            CxPlatListInsertTail(RecvBuffer.Chunks, NewChunk.Link);

            if (!LastChunk.ExternalReference)
            {
                if (LastChunkIsFirst)
                {
                    int nSpan = QuicRecvBufferGetSpan(RecvBuffer);
                    if (nSpan < LastChunk.Buffer.Length)
                    {
                        nSpan = LastChunk.Buffer.Length;
                    }

                    int LengthTillWrap = LastChunk.Buffer.Length - RecvBuffer.ReadStart;
                    if (nSpan <= LengthTillWrap)
                    {
                        Array.Copy(LastChunk.Buffer, RecvBuffer.ReadStart, NewChunk.Buffer, 0, nSpan);
                    }
                    else
                    {
                        Array.Copy(LastChunk.Buffer, RecvBuffer.ReadStart, NewChunk.Buffer, 0, LengthTillWrap);
                        Array.Copy(LastChunk.Buffer, 0, NewChunk.Buffer, LengthTillWrap, nSpan - LengthTillWrap);
                    }
                    RecvBuffer.ReadStart = 0;
                    RecvBuffer.Capacity = TargetBufferLength;
                }
                else
                {
                    Array.Copy(LastChunk.Buffer, NewChunk.Buffer, LastChunk.Length);
                }

                CxPlatListEntryRemove(LastChunk.Link);
                if (LastChunk != RecvBuffer.PreallocatedChunk)
                {
                    LastChunk = null;
                }

                return true;
            }

            if (RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE)
            {
                return true;
            }

            int nSpanLength = QuicRecvBufferGetSpan(RecvBuffer);
            int nLengthTillWrap = LastChunk.Buffer.Length - RecvBuffer.ReadStart;
            if (nSpanLength <= nLengthTillWrap)
            {
                Array.Copy(LastChunk.Buffer, RecvBuffer.ReadStart, NewChunk.Buffer, 0, nSpanLength);
            }
            else
            {
                Array.Copy(LastChunk.Buffer, RecvBuffer.ReadStart, NewChunk.Buffer, 0, nLengthTillWrap);
                Array.Copy(LastChunk.Buffer, 0, NewChunk.Buffer, nLengthTillWrap, nSpanLength - nLengthTillWrap);
            }
            RecvBuffer.ReadStart = 0;
            return true;
        }

        static void QuicRecvBufferIncreaseVirtualBufferLength(QUIC_RECV_BUFFER RecvBuffer, int NewLength)
        {
            NetLog.Assert(NewLength >= RecvBuffer.VirtualBufferLength);
            RecvBuffer.VirtualBufferLength = NewLength;
        }

    }

}

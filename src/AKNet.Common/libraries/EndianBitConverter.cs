/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:14
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet.LinuxTcp")]
[assembly: InternalsVisibleTo("AKNet.WebSocket")]
[assembly: InternalsVisibleTo("SimpleTest")]
namespace AKNet.Common
{
    //这里默认使用大端存储的
    internal static class EndianBitConverter
    {
        private const bool bUseBinaryPrimitives = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, UInt64 value)
        {
            if (bUseBinaryPrimitives)
            {
                BinaryPrimitives.WriteUInt64BigEndian(mBuffer.Slice(nBeginIndex), value);
            }
            else
            {
                mBuffer[nBeginIndex + 0] = (byte)(value >> 56);
                mBuffer[nBeginIndex + 1] = (byte)(value >> 48);
                mBuffer[nBeginIndex + 2] = (byte)(value >> 40);
                mBuffer[nBeginIndex + 3] = (byte)(value >> 32);
                mBuffer[nBeginIndex + 4] = (byte)(value >> 24);
                mBuffer[nBeginIndex + 5] = (byte)(value >> 16);
                mBuffer[nBeginIndex + 6] = (byte)(value >> 8);
                mBuffer[nBeginIndex + 7] = (byte)(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, Int32 value)
        {
            if (bUseBinaryPrimitives)
            {
                BinaryPrimitives.WriteInt32BigEndian(mBuffer.Slice(nBeginIndex), value);
            }
            else
            {
                mBuffer[nBeginIndex + 0] = (byte)(value >> 24);
                mBuffer[nBeginIndex + 1] = (byte)(value >> 16);
                mBuffer[nBeginIndex + 2] = (byte)(value >> 8);
                mBuffer[nBeginIndex + 3] = (byte)(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, UInt32 value)
        {
            if (bUseBinaryPrimitives)
            {
                BinaryPrimitives.WriteUInt32BigEndian(mBuffer.Slice(nBeginIndex), value);
            }
            else
            {
                mBuffer[nBeginIndex + 0] = (byte)(value >> 24);
                mBuffer[nBeginIndex + 1] = (byte)(value >> 16);
                mBuffer[nBeginIndex + 2] = (byte)(value >> 8);
                mBuffer[nBeginIndex + 3] = (byte)(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, UInt16 value)
        {
            if (bUseBinaryPrimitives)
            {
                BinaryPrimitives.WriteUInt16BigEndian(mBuffer.Slice(nBeginIndex), value);
            }
            else
            {
                mBuffer[nBeginIndex + 0] = (byte)(value >> 8);
                mBuffer[nBeginIndex + 1] = (byte)(value);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, Int32 value)
        {
            if (bUseBinaryPrimitives)
            {
                BinaryPrimitives.WriteInt32BigEndian(mBuffer.AsSpan().Slice(nBeginIndex), value);
            }
            else
            {
                mBuffer[nBeginIndex + 0] = (byte)(value >> 24);
                mBuffer[nBeginIndex + 1] = (byte)(value >> 16);
                mBuffer[nBeginIndex + 2] = (byte)(value >> 8);
                mBuffer[nBeginIndex + 3] = (byte)value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, UInt32 value)
        {
            if (bUseBinaryPrimitives)
            {
                BinaryPrimitives.WriteUInt32BigEndian(mBuffer.AsSpan().Slice(nBeginIndex), value);
            }
            else
            {
                mBuffer[nBeginIndex + 0] = (byte)(value >> 24);
                mBuffer[nBeginIndex + 1] = (byte)(value >> 16);
                mBuffer[nBeginIndex + 2] = (byte)(value >> 8);
                mBuffer[nBeginIndex + 3] = (byte)value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, UInt16 value)
        {
            if (bUseBinaryPrimitives)
            {
                BinaryPrimitives.WriteUInt16BigEndian(mBuffer.AsSpan().Slice(nBeginIndex), value);
            }
            else
            {
                mBuffer[nBeginIndex + 0] = (byte)(value >> 8);
                mBuffer[nBeginIndex + 1] = (byte)value;
            }
        }


        //--------------------------------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(byte[] mBuffer, int nBeginIndex)
        {
            if (bUseBinaryPrimitives)
            {
                return BinaryPrimitives.ReadUInt16BigEndian(mBuffer.AsSpan().Slice(nBeginIndex));
            }
            else
            {
                return (ushort)(mBuffer[nBeginIndex + 0] << 8 | mBuffer[nBeginIndex + 1]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(byte[] mBuffer, int nBeginIndex)
        {
            if (bUseBinaryPrimitives)
            {
                return BinaryPrimitives.ReadUInt32BigEndian(mBuffer.AsSpan().Slice(nBeginIndex));
            }
            else
            {
                return (uint)mBuffer[nBeginIndex + 0] << 24 |
                    (uint)mBuffer[nBeginIndex + 1] << 16 |
                    (uint)mBuffer[nBeginIndex + 2] << 8 |
                    (uint)mBuffer[nBeginIndex + 3];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(byte[] mBuffer, int nBeginIndex)
        {
            if (bUseBinaryPrimitives)
            {
                return BinaryPrimitives.ReadInt32BigEndian(mBuffer.AsSpan().Slice(nBeginIndex));
            }
            else
            {
                return (int)(
                    mBuffer[nBeginIndex + 0] << 24 |
                    mBuffer[nBeginIndex + 1] << 16 |
                    mBuffer[nBeginIndex + 2] << 8 |
                    mBuffer[nBeginIndex + 3]
                    );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(byte[] mBuffer, int nBeginIndex)
        {
            if (bUseBinaryPrimitives)
            {
                return BinaryPrimitives.ReadUInt64BigEndian(mBuffer.AsSpan().Slice(nBeginIndex));
            }
            else
            {
                return
                    (ulong)mBuffer[nBeginIndex + 0] << 56 |
                    (ulong)mBuffer[nBeginIndex + 1] << 48 |
                    (ulong)mBuffer[nBeginIndex + 2] << 40 |
                    (ulong)mBuffer[nBeginIndex + 3] << 32 |
                    (ulong)mBuffer[nBeginIndex + 4] << 24 |
                    (ulong)mBuffer[nBeginIndex + 5] << 16 |
                    (ulong)mBuffer[nBeginIndex + 6] << 8 |
                    (ulong)mBuffer[nBeginIndex + 7];
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            if (bUseBinaryPrimitives)
            {
                return BinaryPrimitives.ReadUInt16BigEndian(mBuffer.Slice(nBeginIndex));
            }
            else
            {
                return (ushort)(mBuffer[0 + nBeginIndex] << 8 | mBuffer[1 + nBeginIndex]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            if (bUseBinaryPrimitives)
            {
                return BinaryPrimitives.ReadInt32BigEndian(mBuffer.Slice(nBeginIndex));
            }
            else
            {
                return (int)mBuffer[0 + nBeginIndex] << 24 |
                    (int)mBuffer[1 + nBeginIndex] << 16 |
                    (int)mBuffer[2 + nBeginIndex] << 8 |
                    (int)mBuffer[3 + nBeginIndex];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            if (bUseBinaryPrimitives)
            {
                return BinaryPrimitives.ReadUInt32BigEndian(mBuffer.Slice(nBeginIndex));
            }
            else
            {
                return
                    (uint)mBuffer[0 + nBeginIndex] << 24 |
                    (uint)mBuffer[1 + nBeginIndex] << 16 |
                    (uint)mBuffer[2 + nBeginIndex] << 8 |
                    (uint)mBuffer[3 + nBeginIndex];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            if (bUseBinaryPrimitives)
            {
                return BinaryPrimitives.ReadUInt64BigEndian(mBuffer.Slice(nBeginIndex));
            }
            else
            {
                return
                    (ulong)mBuffer[0 + nBeginIndex] << 56 |
                    (ulong)mBuffer[1 + nBeginIndex] << 48 |
                    (ulong)mBuffer[2 + nBeginIndex] << 40 |
                    (ulong)mBuffer[3 + nBeginIndex] << 32 |
                    (ulong)mBuffer[4 + nBeginIndex] << 24 |
                    (ulong)mBuffer[5 + nBeginIndex] << 16 |
                    (ulong)mBuffer[6 + nBeginIndex] << 8 |
                    (ulong)mBuffer[7 + nBeginIndex];
            }
        }




        //---------------------------------扩展-------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, string value)
        {
            ReadOnlySpan<char> chars = value.AsSpan();
            for (int i = 0; i < chars.Length; i++)
            {
                mBuffer[nBeginIndex + i] = (byte)chars[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(AkCircularBuffer mBuffer, int nBeginIndex)
        {
            return (ushort)(mBuffer[nBeginIndex + 0] << 8 | mBuffer[nBeginIndex + 1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(AkCircularBuffer mBuffer, int nBeginIndex)
        {
            return (int)(
                mBuffer[nBeginIndex + 0] << 24 |
                mBuffer[nBeginIndex + 1] << 16 |
                mBuffer[nBeginIndex + 2] << 8 |
                mBuffer[nBeginIndex + 3]
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(AkCircularBuffer mBuffer, int nBeginIndex)
        {
            return
                (uint)mBuffer[nBeginIndex + 0] << 24 |
                (uint)mBuffer[nBeginIndex + 1] << 16 |
                (uint)mBuffer[nBeginIndex + 2] << 8 |
                (uint)mBuffer[nBeginIndex + 3];
        }

    }
}

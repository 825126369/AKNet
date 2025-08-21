/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet2")]
namespace AKNet.Common
{
    //这里默认使用大端存储的
    internal static class EndianBitConverter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, ulong value)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, int value)
        {
            mBuffer[nBeginIndex + 0] = (byte)(value >> 24);
            mBuffer[nBeginIndex + 1] = (byte)(value >> 16 );
            mBuffer[nBeginIndex + 2] = (byte)(value >> 8);
            mBuffer[nBeginIndex + 3] = (byte)(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, uint value)
        {
            mBuffer[nBeginIndex + 0] = (byte)(value >> 24);
            mBuffer[nBeginIndex + 1] = (byte)(value >> 16);
            mBuffer[nBeginIndex + 2] = (byte)(value >> 8);
            mBuffer[nBeginIndex + 3] = (byte)(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, ushort value)
        {
            mBuffer[nBeginIndex + 0] = (byte)(value >> 8);
            mBuffer[nBeginIndex + 1] = (byte)(value);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, int value)
        {
            mBuffer[nBeginIndex + 0] = (byte)(value >> 24);
            mBuffer[nBeginIndex + 1] = (byte)(value >> 16);
            mBuffer[nBeginIndex + 2] = (byte)(value >> 8);
            mBuffer[nBeginIndex + 3] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, uint value)
        {
            mBuffer[nBeginIndex + 0] = (byte)(value >> 24);
            mBuffer[nBeginIndex + 1] = (byte)(value >> 16);
            mBuffer[nBeginIndex + 2] = (byte)(value >> 8);
            mBuffer[nBeginIndex + 3] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, ushort value)
        {
            mBuffer[nBeginIndex + 0] = (byte)(value >> 8);
            mBuffer[nBeginIndex + 1] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, string value)
        {
            ReadOnlySpan<char> chars = value.AsSpan();
            for(int i = 0; i < chars.Length; i++)
            {
                mBuffer[nBeginIndex + i] = (byte)chars[i];
            }
        }


        //--------------------------------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(byte[] mBuffer, int nBeginIndex)
        {
            return (ushort)(mBuffer[nBeginIndex + 0] << 8 | mBuffer[nBeginIndex + 1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(byte[] mBuffer, int nBeginIndex)
        {
            return
                (uint)mBuffer[nBeginIndex + 0] << 24 |
                (uint)mBuffer[nBeginIndex + 1] << 16 |
                (uint)mBuffer[nBeginIndex + 2] << 8 |
                (uint)mBuffer[nBeginIndex + 3];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(byte[] mBuffer, int nBeginIndex)
        {
            return (int)(
                mBuffer[nBeginIndex + 0] << 24 |
                mBuffer[nBeginIndex + 1] << 16 |
                mBuffer[nBeginIndex + 2] << 8 |
                mBuffer[nBeginIndex + 3]
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(byte[] mBuffer, int nBeginIndex)
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
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            return (ushort)(mBuffer[0 + nBeginIndex] << 8 | mBuffer[1 + nBeginIndex]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            return (int)mBuffer[0 + nBeginIndex] << 24 |
                (int)mBuffer[1 + nBeginIndex] << 16 |
                (int)mBuffer[2 + nBeginIndex] << 8 |
                (int)mBuffer[3 + nBeginIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            return
                (uint)mBuffer[0 + nBeginIndex] << 24 |
                (uint)mBuffer[1 + nBeginIndex] << 16 |
                (uint)mBuffer[2 + nBeginIndex] << 8 |
                (uint)mBuffer[3 + nBeginIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
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
}

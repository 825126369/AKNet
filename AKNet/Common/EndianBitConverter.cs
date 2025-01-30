/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:22
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Runtime.CompilerServices;

namespace AKNet.Common
{
    //这里默认使用小端存储的，主要是方便
    internal static class EndianBitConverter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, int value)
        {
            mBuffer[nBeginIndex + 0] = (byte)value;
            mBuffer[nBeginIndex + 1] = (byte)(value >> 8);
            mBuffer[nBeginIndex + 2] = (byte)(value >> 16);
            mBuffer[nBeginIndex + 3] = (byte)(value >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, uint value)
        {
            mBuffer[nBeginIndex + 0] = (byte)value;
            mBuffer[nBeginIndex + 1] = (byte)(value >> 8);
            mBuffer[nBeginIndex + 2] = (byte)(value >> 16);
            mBuffer[nBeginIndex + 3] = (byte)(value >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(Span<byte> mBuffer, int nBeginIndex, ushort value)
        {
            mBuffer[nBeginIndex + 0] = (byte)value;
            mBuffer[nBeginIndex + 1] = (byte)(value >> 8);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, int value)
        {
            mBuffer[nBeginIndex + 0] = (byte)value;
            mBuffer[nBeginIndex + 1] = (byte)(value >> 8);
            mBuffer[nBeginIndex + 2] = (byte)(value >> 16);
            mBuffer[nBeginIndex + 3] = (byte)(value >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, uint value)
        {
            mBuffer[nBeginIndex + 0] = (byte)value;
            mBuffer[nBeginIndex + 1] = (byte)(value >> 8);
            mBuffer[nBeginIndex + 2] = (byte)(value >> 16);
            mBuffer[nBeginIndex + 3] = (byte)(value >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBytes(byte[] mBuffer, int nBeginIndex, ushort value)
        {
            mBuffer[nBeginIndex + 0] = (byte)value;
            mBuffer[nBeginIndex + 1] = (byte)(value >> 8);
        }




        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(byte[] mBuffer, int nBeginIndex)
        {
            return (ushort)(mBuffer[nBeginIndex + 0] | mBuffer[nBeginIndex + 1] << 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(byte[] mBuffer, int nBeginIndex)
        {
            return (uint)(
                mBuffer[nBeginIndex + 0] |
                mBuffer[nBeginIndex + 1] << 8 |
                mBuffer[nBeginIndex + 2] << 16 |
                mBuffer[nBeginIndex + 3] << 24
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(byte[] mBuffer, int nBeginIndex)
        {
            return (int)(
                mBuffer[nBeginIndex + 0] |
                mBuffer[nBeginIndex + 1] << 8 |
                mBuffer[nBeginIndex + 2] << 16 |
                mBuffer[nBeginIndex + 3] << 24
                );
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(AkCircularBuffer mBuffer, int nBeginIndex)
        {
            return (ushort)(mBuffer[nBeginIndex + 0] | mBuffer[nBeginIndex + 1] << 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(AkCircularBuffer mBuffer, int nBeginIndex)
        {
            return (int)(
                mBuffer[nBeginIndex + 0] |
                mBuffer[nBeginIndex + 1] << 8 |
                mBuffer[nBeginIndex + 2] << 16 |
                mBuffer[nBeginIndex + 3] << 24
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(AkCircularBuffer mBuffer, int nBeginIndex)
        {
            return (uint)(
                mBuffer[nBeginIndex + 0] |
                mBuffer[nBeginIndex + 1] << 8 |
                mBuffer[nBeginIndex + 2] << 16 |
                mBuffer[nBeginIndex + 3] << 24
                );
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            return (ushort)(mBuffer[0 + nBeginIndex] | mBuffer[1 + nBeginIndex] << 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            return (int)(
                mBuffer[0 + nBeginIndex] |
                mBuffer[1 + nBeginIndex] << 8 |
                mBuffer[2 + nBeginIndex] << 16 |
                mBuffer[3 + nBeginIndex] << 24
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            return (uint)(
                mBuffer[0 + nBeginIndex] |
                mBuffer[1 + nBeginIndex] << 8 |
                mBuffer[2 + nBeginIndex] << 16 |
                mBuffer[3 + nBeginIndex] << 24
                );
        }
    }
}

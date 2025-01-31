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
    //这里默认使用大端存储的
    internal static class EndianBitConverter
    {
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



        //--------------------------------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(byte[] mBuffer, int nBeginIndex)
        {
            return (ushort)(mBuffer[nBeginIndex + 0] << 8 | mBuffer[nBeginIndex + 1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(byte[] mBuffer, int nBeginIndex)
        {
            return (uint)(
                mBuffer[nBeginIndex + 0] << 24 |
                mBuffer[nBeginIndex + 1] << 16 |
                mBuffer[nBeginIndex + 2] << 8 |
                mBuffer[nBeginIndex + 3]
                );
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
            return (uint)(
                mBuffer[nBeginIndex + 0] << 24 |
                mBuffer[nBeginIndex + 1] << 16 |
                mBuffer[nBeginIndex + 2] << 8 |
                mBuffer[nBeginIndex + 3]
                );
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            return (ushort)(mBuffer[0 + nBeginIndex] << 8 | mBuffer[1 + nBeginIndex]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            return (int)(
                mBuffer[0 + nBeginIndex] << 24 |
                mBuffer[1 + nBeginIndex] << 16 |
                mBuffer[2 + nBeginIndex] << 8 |
                mBuffer[3 + nBeginIndex]
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(ReadOnlySpan<byte> mBuffer, int nBeginIndex = 0)
        {
            return (uint)(
                mBuffer[0 + nBeginIndex] << 24 |
                mBuffer[1 + nBeginIndex] << 16 |
                mBuffer[2 + nBeginIndex] << 8 |
                mBuffer[3 + nBeginIndex]
                );
        }

    }
}

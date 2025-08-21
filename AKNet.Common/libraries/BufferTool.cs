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
[assembly: InternalsVisibleTo("AKNet.Other")]
namespace AKNet.Common
{
    internal static class BufferTool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnSureBufferOk(ref byte[] mCacheBuffer, int nSumLength)
        {
            if (mCacheBuffer.Length < nSumLength)
            {
                byte[] mOldBuffer = mCacheBuffer;
                int newSize = mOldBuffer.Length * 2;
                while (newSize < nSumLength)
                {
                    newSize *= 2;
                }
                mCacheBuffer = new byte[newSize];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnSureBufferOk2(ref byte[] mCacheBuffer, int nSumLength)
        {
            if (mCacheBuffer.Length < nSumLength)
            {
                mCacheBuffer = new byte[nSumLength];
            }
        }

        public static bool orBufferEqual(ReadOnlySpan<byte> buffer1, ReadOnlySpan<byte> buffer2)
        {
            if (buffer1.Length != buffer2.Length)
            {
                return false;
            }

            for (int i = 0; i < buffer1.Length; i++)
            {
                if (buffer1[i] != buffer2[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static bool orBufferEqual(byte[] buffer1, byte[] buffer2, int nLength)
        {
            return orBufferEqual(buffer1, 0, buffer2, 0, nLength);
        }

        public static bool orBufferEqual(byte[] buffer1, int Offset1, byte[] buffer2, int nOffset2, int nLength)
        {
            if (buffer1.Length - Offset1 < nLength) return false;
            if (buffer2.Length - nOffset2 < nLength) return false;

            for (int i = 0; i < nLength; i++)
            {
                if (buffer1[i + Offset1] != buffer2[i + nOffset2])
                {
                    return false;
                }
            }
            return true;
        }
    }
}

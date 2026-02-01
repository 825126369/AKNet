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
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet.LinuxTcp")]
[assembly: InternalsVisibleTo("AKNet.WebSocket")]
[assembly: InternalsVisibleTo("AKNet.Extentions.Protobuf")]
namespace AKNet.Common
{
    internal static class BufferTool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnSureBufferOk_Power2(ref byte[] mCacheBuffer, int nSumLength)
        {
            NetLog.Assert(CommonFunc.IsPowerOf2(mCacheBuffer.Length));
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
        public static void EnSureBufferOk_JustRight(ref byte[] mCacheBuffer, int nSumLength)
        {
            if (mCacheBuffer.Length < nSumLength)
            {
                mCacheBuffer = new byte[nSumLength];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool orBufferEqual(byte[] buffer1, byte[] buffer2, int nLength)
        {
            return orBufferEqual(
               buffer1.AsSpan().Slice(0, nLength),
               buffer2.AsSpan().Slice(0, nLength));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool orBufferEqual(byte[] buffer1, int Offset1, byte[] buffer2, int nOffset2, int nLength)
        {
            return orBufferEqual(
                buffer1.AsSpan().Slice(Offset1, nLength),
                buffer2.AsSpan().Slice(nOffset2, nLength));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool orBufferEqual(ReadOnlySpan<byte> buffer1, ReadOnlySpan<byte> buffer2)
        {
            if (false)
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
            }
            else
            {
                return buffer1.SequenceEqual(buffer2);
            }
        }
    }
}

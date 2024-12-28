/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:22
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Runtime.CompilerServices;

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
    }
}

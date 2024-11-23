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

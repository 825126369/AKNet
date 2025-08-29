using System;

namespace AKNet.Common
{
    internal static unsafe class UnSafeTool
    {
        public static void DeepCloneStruct<T>(ref T source, ref T target) where T : struct
        {
            int size = sizeof(T);
            // 固定源和目标内存地址
            fixed (T* pSrc = &source)
            fixed (T* pDest = &target)
            {
                Buffer.MemoryCopy(pSrc, pDest, size, size);
            }
        }

        public static ReadOnlySpan<byte> GetSpan<T>(T* source) where T : struct
        {
            return new ReadOnlySpan<byte>((void*)source, sizeof(T));
        }

    }
}

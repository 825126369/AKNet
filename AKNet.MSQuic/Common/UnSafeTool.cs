using System;

namespace AKNet.Common
{
    internal static unsafe class UnSafeTool
    {
        //这个同样不能实现深拷贝啊，和C++ 完全不一样啊
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
    }
}

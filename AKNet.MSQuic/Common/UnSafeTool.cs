/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:46
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
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

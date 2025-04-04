using System;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        public static T CreateInstance<T>() where T : new()
        {
            try
            {
                // 尝试在不触发垃圾回收的情况下分配内存
                if (GC.TryStartNoGCRegion(1024 * 1024)) // 1MB
                {
                    T instance = new T();
                    GC.EndNoGCRegion();
                    return instance;
                }
            }
            catch (OutOfMemoryException)
            {
                // 如果分配失败，返回 null
            }
            return default(T);
        }
    }
}

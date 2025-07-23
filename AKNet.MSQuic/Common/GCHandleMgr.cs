using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AKNet.Common
{
    public sealed class GCHandleMgr
    {
        private readonly Dictionary<IntPtr, GCHandle> handleDic = new Dictionary<IntPtr, GCHandle>();
        private int _currentId = 0;

        private GCHandleMgr() { }

        /// <summary>
        /// 添加一个对象并返回其对应的 IntPtr 句柄
        /// </summary>
        public IntPtr AddObject(object obj)
        {
            lock (handleDic)
            {
                var handle = GCHandle.Alloc(obj, GCHandleType.Normal);
                var ptr = (IntPtr)_currentId++;
                handleDic[ptr] = handle;
                return ptr;
            }
        }

        /// <summary>
        /// 根据 IntPtr 获取对应的托管对象
        /// </summary>
        public object GetObject(IntPtr handle)
        {
            lock (handleDic)
            {
                if (handleDic.TryGetValue(handle, out var gcHandle))
                {
                    return gcHandle.Target;
                }
                return null;
            }
        }

        public void RemoveObject(IntPtr handle)
        {
            lock (handleDic)
            {
                if (handleDic.TryGetValue(handle, out var gcHandle))
                {
                    gcHandle.Free();
                    handleDic.Remove(handle);
                }
            }
        }

        public void Clear()
        {
            lock (handleDic)
            {
                foreach (var pair in handleDic)
                {
                    pair.Value.Free();
                }
                handleDic.Clear();
            }
        }
    }
}

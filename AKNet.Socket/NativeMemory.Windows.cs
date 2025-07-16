// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if WINDOWS
namespace AKNet.Socket
{
    public static unsafe partial class NativeMemory
    {
        [CLSCompliant(false)]
        public static void* AlignedAlloc(nuint byteCount, nuint alignment)
        {
            if (!IsPow2(alignment))
            {
                throw new OutOfMemoryException();
            }

            void* result = Interop.Ucrtbase._aligned_malloc((byteCount != 0) ? byteCount : 1, alignment);
            if (result == null)
            {
                throw new OutOfMemoryException();
            }

            return result;
        }
        
        [CLSCompliant(false)]
        public static void AlignedFree(void* ptr)
        {
            if (ptr != null)
            {
                Interop.Ucrtbase._aligned_free(ptr);
            }
        }
        
        [CLSCompliant(false)]
        public static void* AlignedRealloc(void* ptr, nuint byteCount, nuint alignment)
        {
            if (!IsPow2(alignment))
            {
                throw new OutOfMemoryException();
            }
            
            void* result = Interop.Ucrtbase._aligned_realloc(ptr, (byteCount != 0) ? byteCount : 1, alignment);
            if (result == null)
            {
                throw new OutOfMemoryException();
            }

            return result;
        }
        
        [CLSCompliant(false)]
        public static void* Alloc(nuint byteCount)
        {
            void* result = Interop.Ucrtbase.malloc(byteCount);
            if (result == null)
            {
                throw new OutOfMemoryException();
            }

            return result;
        }
            
        [CLSCompliant(false)]
        public static void* AllocZeroed(nuint elementCount, nuint elementSize)
        {
            void* result = Interop.Ucrtbase.calloc(elementCount, elementSize);

            if (result == null)
            {
                throw new OutOfMemoryException();
            }

            return result;
        }
        
        [CLSCompliant(false)]
        public static void Free(void* ptr)
        {
            if (ptr != null)
            {
                Interop.Ucrtbase.free(ptr);
            }
        }
        
        [CLSCompliant(false)]
        public static void* Realloc(void* ptr, nuint byteCount)
        {
            void* result = Interop.Ucrtbase.realloc(ptr, (byteCount != 0) ? byteCount : 1);
            if (result == null)
            {
                throw new OutOfMemoryException();
            }

            return result;
        }
    }
}
#endif

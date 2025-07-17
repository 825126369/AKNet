#if TARGET_WINDOWS
namespace AKNet.Platform
{
    public static unsafe partial class OSPlatformFunc
    {
        static void* CxPlatAlloc(int ByteCount, uint Tag)
        {
#if DEBUG
            NetLog.Assert(CxPlatform.Heap != IntPtr.Zero);
            NetLog.Assert(ByteCount != 0);
            uint Rand;
            if ((CxPlatform.AllocFailDenominator > 0 && (CxPlatRandom(sizeof(Rand), &Rand), Rand % CxPlatform.AllocFailDenominator) == 1) ||
                (CxPlatform.AllocFailDenominator < 0 && InterlockedIncrement(&CxPlatform.AllocCounter) % CxPlatform.AllocFailDenominator == 0))
            {
                return null;
            }

            void* Alloc = Interop.Kernel32.HeapAlloc(CxPlatform.Heap, 0, ByteCount + AllocOffset);
            if (Alloc == null)
            {
                return null;
            }

            *((uint32_t*)Alloc) = Tag;
            return (void*)((uint8_t*)Alloc + AllocOffset);
#else
    UNREFERENCED_PARAMETER(Tag);
    return HeapAlloc(CxPlatform.Heap, 0, ByteCount);
#endif
        }

        static int CxPlatRandom(int BufferLen, void* Buffer)
        {
            return (int)Interop.BCrypt.BCryptGenRandom(
                    NULL,
                    (uint8_t*)Buffer,
                    BufferLen,
                    BCRYPT_USE_SYSTEM_PREFERRED_RNG);
        }
    }
}

#endif
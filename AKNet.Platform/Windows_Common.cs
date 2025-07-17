#if TARGET_WINDOWS
namespace AKNet.Platform
{
    public static unsafe partial class OSPlatformFunc
    {
        static void* CxPlatAlloc(int ByteCount, uint Tag = 0)
        {
            return Interop.Kernel32.HeapAlloc(CxPlatform.Heap, 0, ByteCount);
        }

        static void CxPlatFree(void* Mem, uint Tag = 0)
        {
            Interop.Kernel32.HeapFree(CxPlatform.Heap, 0, Mem);
        }

        static void CxPlatZeroMemory(void* Destination, int Length)
        {
            Interop.Ucrtbase.memset(Destination, 0, Length);
        }

        static int CxPlatRandom(int BufferLen, void* Buffer)
        {
            const int BCRYPT_RNG_USE_ENTROPY_IN_BUFFER = 0x00000001;
            const int BCRYPT_USE_SYSTEM_PREFERRED_RNG = 0x00000002;

            return (int)Interop.BCrypt.BCryptGenRandom(
                    IntPtr.Zero,
                    (byte*)Buffer,
                    BufferLen,
                    BCRYPT_USE_SYSTEM_PREFERRED_RNG);
        }
    }
}
#endif
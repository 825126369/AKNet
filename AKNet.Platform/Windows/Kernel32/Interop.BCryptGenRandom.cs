using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
#if NET7_0_OR_GREATER
        public static unsafe partial class BCrypt
        {
            internal const int BCRYPT_USE_SYSTEM_PREFERRED_RNG = 0x00000002;

            [LibraryImport(Libraries.BCrypt)]
            public static unsafe partial int BCryptGenRandom(IntPtr hAlgorithm, byte* pbBuffer, int cbBuffer, int dwFlags);
        }
#else
        public static unsafe partial class BCrypt
        {
            [DllImport(Libraries.BCrypt)]
            public static unsafe extern int BCryptGenRandom(IntPtr hAlgorithm, byte* pbBuffer, int cbBuffer, int dwFlags);
        }
#endif
    }
}

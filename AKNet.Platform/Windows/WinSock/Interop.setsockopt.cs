using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
#if NET7_0_OR_GREATER
        public static unsafe partial class Winsock
        {
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static unsafe partial int setsockopt(
                SafeHandle socketHandle,
                int optionLevel,
                int optionName,
                byte* optionValue,
                int optionLength);
        }
#else
        public static unsafe partial class Winsock
        {
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static unsafe extern int setsockopt(
                SafeHandle socketHandle,
                int optionLevel,
                int optionName,
                byte* optionValue,
                int optionLength);
        }
#endif
    }
}

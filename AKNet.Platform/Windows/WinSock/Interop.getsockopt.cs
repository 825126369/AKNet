using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
#if NET7_0_OR_GREATER
        public static unsafe partial class Winsock
        {
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe partial int getsockopt(
                IntPtr socketHandle,
                int optionLevel,
                int optionName,
                byte* optionValue,
                ref int optionLength);
        }
#else
        public static unsafe partial class Winsock
        {
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe extern int getsockopt(
                IntPtr socketHandle,
                int optionLevel,
                int optionName,
                byte* optionValue,
                ref int optionLength);
        }
#endif
    }
}

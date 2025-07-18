using System.Runtime.InteropServices;
namespace AKNet.Platform.Socket
{
    internal static class PaddingHelpers
    {
        internal const int CACHE_LINE_SIZE = 128;
    }
    
    [StructLayout(LayoutKind.Explicit, Size = PaddingHelpers.CACHE_LINE_SIZE - sizeof(int))]
    internal struct PaddingFor32
    {
    }
}

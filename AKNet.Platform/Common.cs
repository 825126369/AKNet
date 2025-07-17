namespace AKNet.Platform
{
    public unsafe class CX_PLATFORM
    {
        public IntPtr Heap;
        public int dwBuildNumber;
#if DEBUG
        public int AllocFailDenominator;
        public long AllocCounter;
#endif
    }

    public static unsafe partial class OSPlatformFunc
    {
        static readonly CX_PLATFORM CxPlatform = new CX_PLATFORM();
    }
}

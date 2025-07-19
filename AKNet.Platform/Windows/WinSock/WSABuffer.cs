using System;
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct WSABUF
    {
        internal int len; // Length of Buffer
        internal IntPtr buf; // Pointer to Buffer
    }
}

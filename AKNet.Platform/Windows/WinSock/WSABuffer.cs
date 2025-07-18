using System;
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct WSABuffer
    {
        internal int Length; // Length of Buffer
        internal IntPtr Pointer; // Pointer to Buffer
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using static AKNet.Socket.Interop;

namespace AKNet.Platform
{
    internal static partial class Interop
    {
#if NET7_0_OR_GREATER
    internal static partial class BCrypt
    {
        internal const int BCRYPT_USE_SYSTEM_PREFERRED_RNG = 0x00000002;

        [LibraryImport(Libraries.BCrypt)]
        public static unsafe partial int BCryptGenRandom(IntPtr hAlgorithm, byte* pbBuffer, int cbBuffer, int dwFlags);
    }
#else
        internal static partial class BCrypt
        {
            [DllImport(Libraries.BCrypt)]
            public static unsafe extern int BCryptGenRandom(IntPtr hAlgorithm, byte* pbBuffer, int cbBuffer, int dwFlags);
        }
#endif
    }
}

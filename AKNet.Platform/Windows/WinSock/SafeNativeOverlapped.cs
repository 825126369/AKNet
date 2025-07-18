using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace AKNet.Platform.Socket
{
    internal sealed class SafeNativeOverlapped : SafeHandle
    {
        private readonly SafeSocketHandle? _socketHandle;

        public SafeNativeOverlapped()
            : this(IntPtr.Zero)
        {
            
        }

        private SafeNativeOverlapped(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public unsafe SafeNativeOverlapped(SafeSocketHandle socketHandle, NativeOverlapped* handle)
            : this((IntPtr)handle)
        {
            _socketHandle = socketHandle;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            FreeNativeOverlapped();
            return true;
        }

        private unsafe void FreeNativeOverlapped()
        {
            IntPtr oldHandle = Interlocked.Exchange(ref handle, IntPtr.Zero);
            if (oldHandle != IntPtr.Zero && !Environment.HasShutdownStarted)
            {
                Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
                Debug.Assert(_socketHandle != null, "_socketHandle is null.");

                ThreadPoolBoundHandle? boundHandle = _socketHandle.IOCPBoundHandle;
                Debug.Assert(boundHandle != null, "SafeNativeOverlapped::FreeNativeOverlapped - boundHandle is null");
                boundHandle?.FreeNativeOverlapped((NativeOverlapped*)oldHandle);
            }
        }
    }
}

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    internal sealed class SafeNativeOverlapped : SafeHandle
    {
        private readonly IntPtr _socketHandle;

        public SafeNativeOverlapped()
            : this(IntPtr.Zero)
        {
            
        }

        private SafeNativeOverlapped(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public unsafe SafeNativeOverlapped(IntPtr socketHandle, NativeOverlapped* handle)
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
           
        }
    }
}

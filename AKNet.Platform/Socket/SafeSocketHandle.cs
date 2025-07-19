using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace AKNet.Platform.Socket
{
    public sealed partial class SafeSocketHandle : SafeHandleMinusOneIsInvalid
    {
        public SafeSocketHandle() : base(ownsHandle: true) => OwnsHandle = true;
        public SafeSocketHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle: true)
        {
            OwnsHandle = ownsHandle; // Track if the SafesocketHandle is owning.
            SetHandleAndValid(preexistingHandle);
        }

        internal bool OwnsHandle { get; }
        internal bool HasShutdownSend => _hasShutdownSend;
        private volatile bool _released;
        private bool _hasShutdownSend;
        
        public override bool IsInvalid => IsClosed || base.IsInvalid;

        protected override bool ReleaseHandle()
        {
            _released = true;
            bool shouldClose = TryOwnClose();
            if (shouldClose)
            {
                CloseHandle(abortive: true, canceledOperations: false);
            }
            return true;
        }

        internal void CloseAsIs(bool abortive)
        {
            bool shouldClose = TryOwnClose();
            Dispose();
            if (shouldClose)
            {
                bool canceledOperations = false;
                SpinWait sw = default;
                while (!_released)
                {
                    canceledOperations |= TryUnblockSocket(abortive);
                    sw.SpinOnce();
                }
                CloseHandle(abortive, canceledOperations);
            }
        }

        private bool CloseHandle(bool abortive, bool canceledOperations)
        {
            bool ret = false;
            canceledOperations |= OnHandleClose();
            if (canceledOperations && !_hasShutdownSend)
            {
                abortive = true;
            }

            ret = !OwnsHandle || DoCloseHandle(abortive) == SocketError.Success;
            return ret;
        }

        private void SetHandleAndValid(IntPtr handle)
        {
            Debug.Assert(!IsClosed);
            base.SetHandle(handle);
            if (IsInvalid)
            {
                TryOwnClose();
                SetHandleAsInvalid();
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace AKNet.Socket
{
    public static partial class ThreadPool
    {
        private static unsafe bool UnsafeQueueNativeOverlappedPortableCore(NativeOverlapped* overlapped)
        {
            if (overlapped == null)
            {
                throw new ArgumentNullException();
            }

            overlapped->InternalLow = IntPtr.Zero;
            PortableThreadPool.ThreadPoolInstance.QueueNativeOverlapped(overlapped);
            return true;
        }

        private static bool BindHandlePortableCore(SafeHandle osHandle)
        {
            if (osHandle == null)
            {
                throw new ArgumentNullException();
            }

            bool mustReleaseSafeHandle = false;
            try
            {
                osHandle.DangerousAddRef(ref mustReleaseSafeHandle);
                PortableThreadPool.ThreadPoolInstance.RegisterForIOCompletionNotifications(osHandle.DangerousGetHandle());
                return true;
            }
            finally
            {
                if (mustReleaseSafeHandle)
                    osHandle.DangerousRelease();
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if LINUX
using System.ComponentModel;
namespace AKNet.Platform.Socket
{
    public partial class SocketException : Win32Exception
    {
        /// <summary>Creates a new instance of the <see cref='System.Net.Sockets.SocketException'/> class with the default error code.</summary>
        public SocketException() : this(Interop.Sys.GetLastErrorInfo())
        {
        }

        internal SocketException(SocketError errorCode, uint platformError) : base((int)platformError)
        {
            _errorCode = errorCode;
        }

        private SocketException(Interop.ErrorInfo error) : this(SocketErrorPal.GetSocketErrorForNativeError(error.Error), (uint)error.RawErrno)
        {
        }
        
        private static int GetNativeErrorForSocketError(SocketError error)
        {
            int nativeErr = (int)error;

            // If an interop error was not found, then don't invoke Info().RawErrno as that will fail with assert.
            if (SocketErrorPal.TryGetNativeErrorForSocketError(error, out Interop.Error interopErr))
            {
                nativeErr = interopErr.Info().RawErrno;
            }

            return nativeErr;
        }
    }
}
#endif
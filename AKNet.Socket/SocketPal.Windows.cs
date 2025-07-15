using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

#if BIGENDIAN
using System.Buffers.Binary;
#endif

namespace AKNet.Socket
{
    internal static class SocketPal
    {
        public const bool SupportsMultipleConnectAttempts = true;

        private static void MicrosecondsToTimeValue(long microseconds, ref Interop.Winsock.TimeValue socketTime)
        {
            const int microcnv = 1000000;

            socketTime.Seconds = (int)(microseconds / microcnv);
            socketTime.Microseconds = (int)(microseconds % microcnv);
        }

        public static SocketError GetLastSocketError()
        {
            int win32Error = Marshal.GetLastWin32Error();
            Debug.Assert(win32Error != 0, "Expected non-0 error");
            return (SocketError)win32Error;
        }

        public static SocketError CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, out SafeSocketHandle socket)
        {
            Interop.Winsock.EnsureInitialized();

            socket = new SafeSocketHandle();
            IntPtr mPtr = Interop.Winsock.WSASocketW(addressFamily, (int)socketType, (int)protocolType, IntPtr.Zero, 0, 
                (int)Interop.Winsock.SocketConstructorFlags.WSA_FLAG_OVERLAPPED | (int)Interop.Winsock.SocketConstructorFlags.WSA_FLAG_NO_HANDLE_INHERIT);

            socket.SetHandle(mPtr);
            if (socket.IsInvalid)
            {
                SocketError error = GetLastSocketError();
                socket.Dispose();
                return error;
            }
            return SocketError.Success;
        }

        public static SocketError SetBlocking(SafeSocketHandle handle, bool shouldBlock, out bool willBlock)
        {
            int intBlocking = shouldBlock ? 0 : -1;

            SocketError errorCode;
            errorCode = Interop.Winsock.ioctlsocket(handle, Interop.Winsock.IoctlSocketConstants.FIONBIO, ref intBlocking);
            if (errorCode == SocketError.SocketError)
            {
                errorCode = GetLastSocketError();
            }

            willBlock = intBlocking == 0;
            return errorCode;
        }

        public static unsafe SocketError GetSockName(SafeSocketHandle handle, byte* buffer, out int nameLen)
        {
            SocketError errorCode = Interop.Winsock.getsockname(handle, buffer, out nameLen);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError GetAvailable(SafeSocketHandle handle, out int available)
        {
            int value = 0;
            SocketError errorCode = Interop.Winsock.ioctlsocket(handle, Interop.Winsock.IoctlSocketConstants.FIONREAD, ref value);
            available = value;
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError GetPeerName(SafeSocketHandle handle, Span<byte> buffer, ref int nameLen)
        {
            fixed (byte* rawBuffer = buffer)
            {
                SocketError errorCode = Interop.Winsock.getpeername(handle, rawBuffer, ref nameLen);
                return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
            }
        }

        public static SocketError Bind(SafeSocketHandle handle, ProtocolType _ /*socketProtocolType*/, ReadOnlySpan<byte> buffer)
        {
            SocketError errorCode = Interop.Winsock.bind(handle, buffer);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError Connect(SafeSocketHandle handle, Memory<byte> peerAddress)
        {
            SocketError errorCode = Interop.Winsock.WSAConnect(
                handle,
                peerAddress.Span,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe IPPacketInformation GetIPPacketInformation(Interop.Winsock.ControlData* controlBuffer)
        {
            IPAddress address = controlBuffer->length == UIntPtr.Zero ? IPAddress.None : new IPAddress((long)controlBuffer->address);
            return new IPPacketInformation(address, (int)controlBuffer->index);
        }

        public static unsafe IPPacketInformation GetIPPacketInformation(Interop.Winsock.ControlDataIPv6* controlBuffer)
        {
            if (controlBuffer->length == (UIntPtr)sizeof(Interop.Winsock.ControlData))
            {
                return GetIPPacketInformation((Interop.Winsock.ControlData*)controlBuffer);
            }

            IPAddress address = controlBuffer->length != UIntPtr.Zero ? new IPAddress(new ReadOnlySpan<byte>(controlBuffer->address, Interop.Winsock.IPv6AddressLength)) : IPAddress.IPv6None;
            return new IPPacketInformation(address, (int)controlBuffer->index);
        }

        public static SocketError WindowsIoctl(SafeSocketHandle handle, int ioControlCode, byte[]? optionInValue, byte[]? optionOutValue, out int optionLength)
        {
            if (ioControlCode == Interop.Winsock.IoctlSocketConstants.FIONBIO)
            {
                throw new InvalidOperationException();
            }

            SocketError errorCode = Interop.Winsock.WSAIoctl_Blocking(
                handle,
                ioControlCode,
                optionInValue,
                optionInValue != null ? optionInValue.Length : 0,
                optionOutValue,
                optionOutValue != null ? optionOutValue.Length : 0,
                out optionLength,
                IntPtr.Zero,
                IntPtr.Zero);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError SetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
        {
            SocketError errorCode;
            errorCode = Interop.Winsock.setsockopt(
                handle,
                optionLevel,
                optionName,
                ref optionValue,
                sizeof(int));

            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError SetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            SocketError errorCode;
            fixed (byte* optionValuePtr = optionValue)
            {
                errorCode = Interop.Winsock.setsockopt(
                    handle,
                    optionLevel,
                    optionName,
                    optionValuePtr,
                    optionValue != null ? optionValue.Length : 0);
                return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
            }
        }

        public static unsafe SocketError SetRawSockOpt(SafeSocketHandle handle, int optionLevel, int optionName, ReadOnlySpan<byte> optionValue)
        {
            fixed (byte* optionValuePtr = optionValue)
            {
                SocketError errorCode = Interop.Winsock.setsockopt(
                    handle,
                    (SocketOptionLevel)optionLevel,
                    (SocketOptionName)optionName,
                    optionValuePtr,
                    optionValue.Length);
                return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
            }
        }

        public static void SetReceivingDualModeIPv4PacketInformation(AKNetSocket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
        }
        
        public static SocketError SetLingerOption(SafeSocketHandle handle, LingerOption optionValue)
        {
            Interop.Winsock.Linger lngopt = default;
            lngopt.OnOff = optionValue.Enabled ? (ushort)1 : (ushort)0;
            lngopt.Time = (ushort)optionValue.LingerTime;
            SocketError errorCode = Interop.Winsock.setsockopt(handle, SocketOptionLevel.Socket, SocketOptionName.Linger, ref lngopt, 4);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static void SetIPProtectionLevel(AKNetSocket socket, SocketOptionLevel optionLevel, int protectionLevel)
        {
            socket.SetSocketOption(optionLevel, SocketOptionName.IPProtectionLevel, protectionLevel);
        }

        public static unsafe SocketError GetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, out int optionValue)
        {
            int optionLength = sizeof(int);
            int tmpOptionValue = 0;
            SocketError errorCode = Interop.Winsock.getsockopt(
                handle,
                optionLevel,
                optionName,
                (byte*)&tmpOptionValue,
                ref optionLength);

            optionValue = tmpOptionValue;
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError GetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue, ref int optionLength)
        {
            fixed (byte* optionValuePtr = optionValue)
            {
                SocketError errorCode = Interop.Winsock.getsockopt(
                   handle,
                   optionLevel,
                   optionName,
                   optionValuePtr,
                   ref optionLength);
                return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
            }
        }

        public static unsafe SocketError GetRawSockOpt(SafeSocketHandle handle, int optionLevel, int optionName, Span<byte> optionValue, ref int optionLength)
        {
            Debug.Assert((uint)optionLength <= optionValue.Length);

            SocketError errorCode;
            fixed (byte* optionValuePtr = optionValue)
            {
                errorCode = Interop.Winsock.getsockopt(
                    handle,
                    (SocketOptionLevel)optionLevel,
                    (SocketOptionName)optionName,
                    optionValuePtr,
                    ref optionLength);
                return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
            }
        }

        public static SocketError GetLingerOption(SafeSocketHandle handle, out LingerOption? optionValue)
        {
            int optlen = 4;
            SocketError errorCode = Interop.Winsock.getsockopt(handle, SocketOptionLevel.Socket, SocketOptionName.Linger, out Interop.Winsock.Linger lngopt, ref optlen);
            if (errorCode == SocketError.SocketError)
            {
                optionValue = default(LingerOption);
                return GetLastSocketError();
            }

            optionValue = new LingerOption(lngopt.OnOff != 0, (int)lngopt.Time);
            return SocketError.Success;
        }

        public static SocketError Shutdown(SafeSocketHandle handle, bool isConnected, bool isDisconnected, SocketShutdown how)
        {
            SocketError err = Interop.Winsock.shutdown(handle, (int)how);
            if (err != SocketError.SocketError)
            {
                return SocketError.Success;
            }

            err = GetLastSocketError();
            Debug.Assert(err != SocketError.NotConnected || (!isConnected && !isDisconnected));
            return err;
        }

        internal static unsafe bool HasNonBlockingConnectCompleted(SafeSocketHandle handle, out bool success)
        {
            bool refAdded = false;
            try
            {
                handle.DangerousAddRef(ref refAdded);

                IntPtr rawHandle = handle.DangerousGetHandle();
                IntPtr* writefds = stackalloc IntPtr[2] { (IntPtr)1, rawHandle };
                IntPtr* exceptfds = stackalloc IntPtr[2] { (IntPtr)1, rawHandle };
                Interop.Winsock.TimeValue timeout = default;
                MicrosecondsToTimeValue(0, ref timeout);

                int socketCount = Interop.Winsock.select(
                            0,
                            null,
                            writefds,
                            exceptfds,
                            ref timeout);

                if ((SocketError)socketCount == SocketError.SocketError)
                {
                    throw new SocketException((int)GetLastSocketError());
                }

                // Failure of the connect attempt is indicated in exceptfds.
                if ((int)exceptfds[0] != 0 && exceptfds[1] == rawHandle)
                {
                    success = false;
                    return true;
                }

                // Success is indicated in writefds.
                if ((int)writefds[0] != 0 && writefds[1] == rawHandle)
                {
                    success = true;
                    return true;
                }

                success = false;
                return false;
            }
            finally
            {
                if (refAdded)
                {
                    handle.DangerousRelease();
                }
            }
        }
    }
}

#if TARGET_WINDOWS

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    public static class SocketPal
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

        public static SocketError CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, out IntPtr socket)
        {
            Interop.Winsock.EnsureInitialized();
            socket = Interop.Winsock.WSASocketW(addressFamily, (int)socketType, (int)protocolType, IntPtr.Zero, 0, 
                (int)Interop.Winsock.SocketConstructorFlags.WSA_FLAG_OVERLAPPED | (int)Interop.Winsock.SocketConstructorFlags.WSA_FLAG_NO_HANDLE_INHERIT);
            
            if (socket == IntPtr.Zero)
            {
                SocketError error = GetLastSocketError();
                return error;
            }
            return SocketError.Success;
        }

        public static SocketError SetBlocking(IntPtr handle, bool shouldBlock, out bool willBlock)
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

        public static unsafe SocketError GetSockName(IntPtr handle, byte* buffer, out int nameLen)
        {
            SocketError errorCode = Interop.Winsock.getsockname(handle, buffer, out nameLen);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError GetAvailable(IntPtr handle, out int available)
        {
            int value = 0;
            SocketError errorCode = Interop.Winsock.ioctlsocket(handle, Interop.Winsock.IoctlSocketConstants.FIONREAD, ref value);
            available = value;
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError GetPeerName(IntPtr handle, Span<byte> buffer, ref int nameLen)
        {
            fixed (byte* rawBuffer = buffer)
            {
                SocketError errorCode = Interop.Winsock.getpeername(handle, rawBuffer, ref nameLen);
                return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
            }
        }

        public static SocketError Bind(IntPtr handle, ProtocolType _ /*socketProtocolType*/, ReadOnlySpan<byte> buffer)
        {
            SocketError errorCode = Interop.Winsock.bind(handle, buffer);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError Connect(IntPtr handle, Memory<byte> peerAddress)
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

        public static SocketError WindowsIoctl(IntPtr handle, int ioControlCode, byte[]? optionInValue, byte[]? optionOutValue, out int optionLength)
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

        public static unsafe SocketError SetSockOpt(IntPtr handle, SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
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

        public static unsafe SocketError SetSockOpt(IntPtr handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
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

        public static unsafe SocketError SetRawSockOpt(IntPtr handle, int optionLevel, int optionName, ReadOnlySpan<byte> optionValue)
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

        public static void SetReceivingDualModeIPv4PacketInformation(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
        }
        
        public static SocketError SetLingerOption(IntPtr handle, LingerOption optionValue)
        {
            Interop.Winsock.Linger lngopt = default;
            lngopt.OnOff = optionValue.Enabled ? (ushort)1 : (ushort)0;
            lngopt.Time = (ushort)optionValue.LingerTime;
            SocketError errorCode = Interop.Winsock.setsockopt(handle, SocketOptionLevel.Socket, SocketOptionName.Linger, ref lngopt, 4);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static void SetIPProtectionLevel(Socket socket, SocketOptionLevel optionLevel, int protectionLevel)
        {
            socket.SetSocketOption(optionLevel, SocketOptionName.IPProtectionLevel, protectionLevel);
        }

        public static unsafe SocketError GetSockOpt(IntPtr handle, SocketOptionLevel optionLevel, SocketOptionName optionName, out int optionValue)
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

        public static unsafe SocketError GetSockOpt(IntPtr handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue, ref int optionLength)
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

        public static unsafe SocketError GetRawSockOpt(IntPtr handle, int optionLevel, int optionName, Span<byte> optionValue, ref int optionLength)
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

        public static SocketError GetLingerOption(IntPtr handle, out LingerOption? optionValue)
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

        public static SocketError Shutdown(IntPtr handle, bool isConnected, bool isDisconnected, SocketShutdown how)
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
    }
}
#endif
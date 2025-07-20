#if TARGET_WINDOWS

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    public static class SocketPal
    {
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

        public static SocketError CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, out SafeHandle socket)
        {
            Interop.Winsock.EnsureInitialized();
            socket = Interop.Winsock.WSASocketW((int)addressFamily, (int)socketType, (int)protocolType, IntPtr.Zero, 0, 
                (int)Interop.Winsock.SocketConstructorFlags.WSA_FLAG_OVERLAPPED | (int)Interop.Winsock.SocketConstructorFlags.WSA_FLAG_NO_HANDLE_INHERIT);
            
            if (socket == null)
            {
                SocketError error = GetLastSocketError();
                return error;
            }
            return SocketError.Success;
        }

        public static unsafe SocketError GetSockName(SafeHandle handle, byte* buffer, out int nameLen)
        {
            SocketError errorCode = (SocketError)Interop.Winsock.getsockname(handle, buffer, out nameLen);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError GetPeerName(SafeHandle handle, Span<byte> buffer, ref int nameLen)
        {
            fixed (byte* rawBuffer = buffer)
            {
                SocketError errorCode = (SocketError)Interop.Winsock.getpeername(handle, rawBuffer, ref nameLen);
                return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
            }
        }

        public static SocketError Bind(SafeHandle handle, ReadOnlySpan<byte> buffer)
        {
            SocketError errorCode = (SocketError)Interop.Winsock.bind(handle, buffer);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError Connect(IntPtr handle, Memory<byte> peerAddress)
        {
            SocketError errorCode = (SocketError)Interop.Winsock.WSAConnect(
                handle,
                peerAddress.Span,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        internal static unsafe IPPacketInformation GetIPPacketInformation(ControlData* controlBuffer)
        {
            IPAddress address = controlBuffer->length == UIntPtr.Zero ? IPAddress.None : new IPAddress((long)controlBuffer->address);
            return new IPPacketInformation(address, (int)controlBuffer->index);
        }

        internal static unsafe IPPacketInformation GetIPPacketInformation(ControlDataIPv6* controlBuffer)
        {
            if (controlBuffer->length == (UIntPtr)sizeof(ControlData))
            {
                return GetIPPacketInformation((ControlData*)controlBuffer);
            }

            IPAddress address = controlBuffer->length != UIntPtr.Zero ? new IPAddress(new ReadOnlySpan<byte>(controlBuffer->address, Interop.Winsock.IPv6AddressLength)) : IPAddress.IPv6None;
            return new IPPacketInformation(address, (int)controlBuffer->index);
        }

        public static SocketError Shutdown(IntPtr handle, bool isConnected, bool isDisconnected, SocketShutdown how)
        {
            SocketError err = (SocketError)Interop.Winsock.shutdown(handle, (int)how);
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
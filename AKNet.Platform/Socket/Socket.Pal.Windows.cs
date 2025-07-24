//#if TARGET_WINDOWS

//using System.Diagnostics;
//using System.Runtime.InteropServices;

//namespace AKNet.Platform.Socket
//{
//    public unsafe static class SocketPal
//    {
//        private static void MicrosecondsToTimeValue(long microseconds, ref Interop.Winsock.TimeValue socketTime)
//        {
//            const int microcnv = 1000000;
//            socketTime.Seconds = (int)(microseconds / microcnv);
//            socketTime.Microseconds = (int)(microseconds % microcnv);
//        }

//        public static SocketError GetLastSocketError()
//        {
//            int win32Error = Marshal.GetLastWin32Error();
//            Debug.Assert(win32Error != 0, "Expected non-0 error");
//            return (SocketError)win32Error;
//        }

//        public static SocketError CreateSocket(int addressFamily, int socketType, int protocolType, out SafeHandle socket)
//        {
//            socket = null;
//            return SocketError.SocketError;
//            //Interop.Winsock.EnsureInitialized();
//            //socket = Interop.Winsock.WSASocketW((int)addressFamily, (int)socketType, (int)protocolType, IntPtr.Zero, 0, 
//            //    (int)Interop.Winsock.SocketConstructorFlags.WSA_FLAG_OVERLAPPED | (int)Interop.Winsock.SocketConstructorFlags.WSA_FLAG_NO_HANDLE_INHERIT);
            
//            //if (socket == null)
//            //{
//            //    SocketError error = GetLastSocketError();
//            //    return error;
//            //}
//            //return SocketError.Success;
//        }

//        public static unsafe SocketError GetSockName(SafeHandle handle, byte* buffer, out int nameLen)
//        {
//            nameLen = 0;
//            return SocketError.SocketError;
//            //SocketError errorCode = (SocketError)Interop.Winsock.getsockname(handle, buffer, out nameLen);
//            //return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
//        }

//        public static int Bind(SafeHandle handle, ReadOnlySpan<byte> buffer)
//        {
//            fixed (byte* ptr = buffer)
//            {
//                return Interop.Winsock.bind(handle, ptr, buffer.Length);
//            }
//        }

//        public static SocketError Connect(SafeHandle handle, Memory<byte> peerAddress)
//        {
//            SocketError errorCode = (SocketError)Interop.Winsock.WSAConnect(
//                handle,
//                peerAddress.Span,
//                0,
//                IntPtr.Zero,
//                IntPtr.Zero,
//                IntPtr.Zero,
//                IntPtr.Zero);
//            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
//        }

//        internal static unsafe IPPacketInformation GetIPPacketInformation(ControlData* controlBuffer)
//        {
//            IPAddress address = controlBuffer->length == UIntPtr.Zero ? IPAddress.None : new IPAddress((long)controlBuffer->address);
//            return new IPPacketInformation(address, (int)controlBuffer->index);
//        }

//        internal static unsafe IPPacketInformation GetIPPacketInformation(ControlDataIPv6* controlBuffer)
//        {
//            //if (controlBuffer->length == (UIntPtr)sizeof(ControlData))
//            //{
//            //    return GetIPPacketInformation((ControlData*)controlBuffer);
//            //}

//            //IPAddress address = controlBuffer->length != UIntPtr.Zero ? new IPAddress(new ReadOnlySpan<byte>(controlBuffer->address, Interop.Winsock.IPv6AddressLength)) : IPAddress.IPv6None;
//            //return new IPPacketInformation(address, (int)controlBuffer->index);
//            return new IPPacketInformation();
//        }

//        public static SocketError Shutdown(SafeHandle handle, SocketShutdown how)
//        {
//            SocketError err = (SocketError)Interop.Winsock.shutdown(handle, (int)how);
//            if (err != SocketError.SocketError)
//            {
//                return SocketError.Success;
//            }
//            err = GetLastSocketError();
//            return err;
//        }
//    }
//}
//#endif
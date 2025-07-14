// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

#if BIGENDIAN
using System.Buffers.Binary;
#endif

namespace AKNet.Socket
{
    internal static class SocketPal
    {
        public const bool SupportsMultipleConnectAttempts = true;
        public static readonly int MaximumAddressSize = UnixDomainSocketEndPoint.MaxAddressSize;

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

        public static unsafe SocketError CreateSocket(
            SocketInformation socketInformation,
            out SafeSocketHandle socket,
            ref AddressFamily addressFamily,
            ref SocketType socketType,
            ref ProtocolType protocolType)
        {
            if (socketInformation.ProtocolInformation == null || socketInformation.ProtocolInformation.Length < sizeof(Interop.Winsock.WSAPROTOCOL_INFOW))
            {
                throw new ArgumentException(SR.net_sockets_invalid_socketinformation, nameof(socketInformation));
            }

            Interop.Winsock.EnsureInitialized();

            fixed (byte* protocolInfoBytes = socketInformation.ProtocolInformation)
            {
                socket = new SafeSocketHandle();

                // Sockets are non-inheritable in .NET Core.
                // Handle properties like HANDLE_FLAG_INHERIT are not cloned with socket duplication, therefore
                // we need to disable handle inheritance when constructing the new socket handle from Protocol Info.
                // Additionally, it looks like WSA_FLAG_NO_HANDLE_INHERIT has no effect when being used with the Protocol Info
                // variant of WSASocketW, so it is being passed to that call only for consistency.
                // Inheritance is being disabled with SetHandleInformation(...) after the WSASocketW call.
                Marshal.InitHandle(socket, Interop.Winsock.WSASocketW(
                    (AddressFamily)(-1),
                    (SocketType)(-1),
                    (ProtocolType)(-1),
                    (IntPtr)protocolInfoBytes,
                    0,
                    Interop.Winsock.SocketConstructorFlags.WSA_FLAG_OVERLAPPED |
                    Interop.Winsock.SocketConstructorFlags.WSA_FLAG_NO_HANDLE_INHERIT));

                if (socket.IsInvalid)
                {
                    SocketError error = GetLastSocketError();
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"WSASocketW failed with error {error}");
                    socket.Dispose();
                    return error;
                }

                if (!Interop.Kernel32.SetHandleInformation(socket, Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT, 0))
                {
                    // Returning SocketError for consistency, since the call site can deal with conversion, and
                    // the most common SetHandleInformation error (AccessDenied) is included in SocketError anyways:
                    SocketError error = GetLastSocketError();
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"SetHandleInformation failed with error {error}");
                    socket.Dispose();

                    return error;
                }

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, socket);

                Interop.Winsock.WSAPROTOCOL_INFOW* protocolInfo = (Interop.Winsock.WSAPROTOCOL_INFOW*)protocolInfoBytes;
                addressFamily = protocolInfo->iAddressFamily;
                socketType = protocolInfo->iSocketType;
                protocolType = protocolInfo->iProtocol;

                return SocketError.Success;
            }
        }

        public static SocketError SetBlocking(SafeSocketHandle handle, bool shouldBlock, out bool willBlock)
        {
            int intBlocking = shouldBlock ? 0 : -1;

            SocketError errorCode;
            errorCode = Interop.Winsock.ioctlsocket(
                handle,
                Interop.Winsock.IoctlSocketConstants.FIONBIO,
                ref intBlocking);

            if (errorCode == SocketError.SocketError)
            {
                errorCode = GetLastSocketError();
            }

            willBlock = intBlocking == 0;
            return errorCode;
        }

        public static unsafe SocketError GetSockName(SafeSocketHandle handle, byte* buffer, int* nameLen)
        {
            SocketError errorCode = Interop.Winsock.getsockname(handle, buffer, nameLen);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static SocketError GetAvailable(SafeSocketHandle handle, out int available)
        {
            int value = 0;
            SocketError errorCode = Interop.Winsock.ioctlsocket(
                handle,
                Interop.Winsock.IoctlSocketConstants.FIONREAD,
                ref value);
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
                // IPv4 client connectiong to dual mode socket.
                return GetIPPacketInformation((Interop.Winsock.ControlData*)controlBuffer);
            }

            IPAddress address = controlBuffer->length != UIntPtr.Zero ?
                new IPAddress(new ReadOnlySpan<byte>(controlBuffer->address, Interop.Winsock.IPv6AddressLength)) :
                IPAddress.IPv6None;

            return new IPPacketInformation(address, (int)controlBuffer->index);
        }

        public static unsafe SocketError ReceiveMessageFrom(Socket socket, SafeSocketHandle handle, byte[] buffer, int offset, int size, ref SocketFlags socketFlags, SocketAddress socketAddress, out SocketAddress receiveAddress, out IPPacketInformation ipPacketInformation, out int bytesTransferred)
        {
            return ReceiveMessageFrom(socket, handle, new Span<byte>(buffer, offset, size), ref socketFlags, socketAddress, out receiveAddress, out ipPacketInformation, out bytesTransferred);
        }

        public static unsafe SocketError ReceiveMessageFrom(Socket socket, SafeSocketHandle handle, Span<byte> buffer, ref SocketFlags socketFlags, SocketAddress socketAddress, out SocketAddress receiveAddress, out IPPacketInformation ipPacketInformation, out int bytesTransferred)
        {
            bool ipv4, ipv6;
            Socket.GetIPProtocolInformation(socket.AddressFamily, socketAddress, out ipv4, out ipv6);

            bytesTransferred = 0;
            receiveAddress = socketAddress;
            ipPacketInformation = default(IPPacketInformation);
            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer))
            fixed (byte* ptrSocketAddress = &MemoryMarshal.GetReference(socketAddress.Buffer.Span))
            {
                Interop.Winsock.WSAMsg wsaMsg;
                wsaMsg.socketAddress = (IntPtr)ptrSocketAddress;
                wsaMsg.addressLength = (uint)socketAddress.Size;
                wsaMsg.flags = socketFlags;

                WSABuffer wsaBuffer;
                wsaBuffer.Length = buffer.Length;
                wsaBuffer.Pointer = (IntPtr)bufferPtr;
                wsaMsg.buffers = (IntPtr)(&wsaBuffer);
                wsaMsg.count = 1;

                if (ipv4)
                {
                    Interop.Winsock.ControlData controlBuffer;
                    wsaMsg.controlBuffer.Pointer = (IntPtr)(&controlBuffer);
                    wsaMsg.controlBuffer.Length = sizeof(Interop.Winsock.ControlData);

                    if (socket.WSARecvMsgBlocking(
                        handle,
                        (IntPtr)(&wsaMsg),
                        out bytesTransferred) == SocketError.SocketError)
                    {
                        return GetLastSocketError();
                    }

                    ipPacketInformation = GetIPPacketInformation(&controlBuffer);
                }
                else if (ipv6)
                {
                    Interop.Winsock.ControlDataIPv6 controlBuffer;
                    wsaMsg.controlBuffer.Pointer = (IntPtr)(&controlBuffer);
                    wsaMsg.controlBuffer.Length = sizeof(Interop.Winsock.ControlDataIPv6);

                    if (socket.WSARecvMsgBlocking(
                        handle,
                        (IntPtr)(&wsaMsg),
                        out bytesTransferred) == SocketError.SocketError)
                    {
                        return GetLastSocketError();
                    }

                    ipPacketInformation = GetIPPacketInformation(&controlBuffer);
                }
                else
                {
                    wsaMsg.controlBuffer.Pointer = IntPtr.Zero;
                    wsaMsg.controlBuffer.Length = 0;

                    if (socket.WSARecvMsgBlocking(
                        handle,
                        (IntPtr)(&wsaMsg),
                        out bytesTransferred) == SocketError.SocketError)
                    {
                        return GetLastSocketError();
                    }
                }

                socketFlags = wsaMsg.flags;
            }

            return SocketError.Success;
        }

        public static unsafe SocketError ReceiveFrom(SafeSocketHandle handle, byte[] buffer, int offset, int size, SocketFlags _ /*socketFlags*/, Memory<byte> socketAddress, out int addressLength, out int bytesTransferred) =>
            ReceiveFrom(handle, buffer.AsSpan(offset, size), SocketFlags.None, socketAddress, out addressLength, out bytesTransferred);

        public static unsafe SocketError ReceiveFrom(SafeSocketHandle handle, Span<byte> buffer, SocketFlags socketFlags, Memory<byte> socketAddress, out int addressLength, out int bytesTransferred)
        {
            int bytesReceived;

            addressLength = socketAddress.Length;
            bytesReceived = Interop.Winsock.recvfrom(handle, buffer, buffer.Length, socketFlags, socketAddress.Span, ref addressLength);

            if (bytesReceived == (int)SocketError.SocketError)
            {
                bytesTransferred = 0;
                return GetLastSocketError();
            }

            bytesTransferred = bytesReceived;
            return SocketError.Success;
        }

        public static SocketError WindowsIoctl(SafeSocketHandle handle, int ioControlCode, byte[]? optionInValue, byte[]? optionOutValue, out int optionLength)
        {
            if (ioControlCode == Interop.Winsock.IoctlSocketConstants.FIONBIO)
            {
                throw new InvalidOperationException(SR.net_sockets_useblocking);
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
            if (optionLevel == SocketOptionLevel.Tcp &&
                (optionName == SocketOptionName.TcpKeepAliveTime || optionName == SocketOptionName.TcpKeepAliveInterval) &&
                IOControlKeepAlive.IsNeeded)
            {
                errorCode = IOControlKeepAlive.Set(handle, optionName, optionValue);
            }
            else
            {
                errorCode = Interop.Winsock.setsockopt(
                    handle,
                    optionLevel,
                    optionName,
                    ref optionValue,
                    sizeof(int));
            }
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError SetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            SocketError errorCode;
            if (optionLevel == SocketOptionLevel.Tcp &&
                (optionName == SocketOptionName.TcpKeepAliveTime || optionName == SocketOptionName.TcpKeepAliveInterval) &&
                IOControlKeepAlive.IsNeeded)
            {
                return IOControlKeepAlive.Set(handle, optionName, optionValue);
            }

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

        public static void SetReceivingDualModeIPv4PacketInformation(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
        }
        
        public static SocketError SetLingerOption(SafeSocketHandle handle, LingerOption optionValue)
        {
            Interop.Winsock.Linger lngopt = default;
            lngopt.OnOff = optionValue.Enabled ? (ushort)1 : (ushort)0;
            lngopt.Time = (ushort)optionValue.LingerTime;

            // This can throw ObjectDisposedException.
            SocketError errorCode = Interop.Winsock.setsockopt(
                handle,
                SocketOptionLevel.Socket,
                SocketOptionName.Linger,
                ref lngopt,
                4);
            return errorCode == SocketError.SocketError ? GetLastSocketError() : SocketError.Success;
        }

        public static void SetIPProtectionLevel(Socket socket, SocketOptionLevel optionLevel, int protectionLevel)
        {
            socket.SetSocketOption(optionLevel, SocketOptionName.IPProtectionLevel, protectionLevel);
        }

        public static unsafe SocketError GetSockOpt(SafeSocketHandle handle, SocketOptionLevel optionLevel, SocketOptionName optionName, out int optionValue)
        {
            if (optionLevel == SocketOptionLevel.Tcp &&
                (optionName == SocketOptionName.TcpKeepAliveTime || optionName == SocketOptionName.TcpKeepAliveInterval) &&
                IOControlKeepAlive.IsNeeded)
            {
                optionValue = IOControlKeepAlive.Get(handle, optionName);
                return SocketError.Success;
            }

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
            if (optionLevel == SocketOptionLevel.Tcp &&
                (optionName == SocketOptionName.TcpKeepAliveTime || optionName == SocketOptionName.TcpKeepAliveInterval) &&
                IOControlKeepAlive.IsNeeded)
            {
                return IOControlKeepAlive.Get(handle, optionName, optionValue, ref optionLength);
            }

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

            // This can throw ObjectDisposedException.
            SocketError errorCode = Interop.Winsock.getsockopt(
                handle,
                SocketOptionLevel.Socket,
                SocketOptionName.Linger,
                out Interop.Winsock.Linger lngopt,
                ref optlen);

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
                handle.TrackShutdown(how);
                return SocketError.Success;
            }

            err = GetLastSocketError();
            Debug.Assert(err != SocketError.NotConnected || (!isConnected && !isDisconnected));
            return err;
        }

        // This assumes preBuffer/postBuffer are pinned already

        private static unsafe bool TransmitFileHelper(
            SafeHandle socket,
            SafeHandle? fileHandle,
            NativeOverlapped* overlapped,
            IntPtr pinnedPreBuffer,
            int preBufferLength,
            IntPtr pinnedPostBuffer,
            int postBufferLength,
            TransmitFileOptions flags)
        {
            bool needTransmitFileBuffers = false;
            Interop.Mswsock.TransmitFileBuffers transmitFileBuffers = default;

            if (preBufferLength > 0)
            {
                needTransmitFileBuffers = true;
                transmitFileBuffers.Head = pinnedPreBuffer;
                transmitFileBuffers.HeadLength = preBufferLength;
            }

            if (postBufferLength > 0)
            {
                needTransmitFileBuffers = true;
                transmitFileBuffers.Tail = pinnedPostBuffer;
                transmitFileBuffers.TailLength = postBufferLength;
            }

            bool releaseRef = false;
            IntPtr fileHandlePtr = IntPtr.Zero;
            try
            {
                if (fileHandle != null)
                {
                    fileHandle.DangerousAddRef(ref releaseRef);
                    fileHandlePtr = fileHandle.DangerousGetHandle();
                }

                return Interop.Mswsock.TransmitFile(
                    socket, fileHandlePtr, 0, 0, overlapped,
                    needTransmitFileBuffers ? &transmitFileBuffers : null, flags);
            }
            finally
            {
                if (releaseRef)
                {
                    fileHandle!.DangerousRelease();
                }
            }
        }

        [Conditional("unnecessary")]
        public static void CheckDualModePacketInfoSupport(Socket socket)
        {
            // Dual-mode sockets support received packet info on Windows.
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

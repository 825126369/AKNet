using System.Collections;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace AKNet.Platform.Socket
{
    public partial class Socket
    {
        private static CachedSerializedEndPoint? s_cachedAnyEndPoint;
        private static CachedSerializedEndPoint? s_cachedAnyV6EndPoint;
        private static CachedSerializedEndPoint? s_cachedMappedAnyV6EndPoint;

        private sealed class CachedSerializedEndPoint
        {
            public readonly IPEndPoint IPEndPoint;
            public readonly SocketAddress SocketAddress;

            public CachedSerializedEndPoint(IPAddress address)
            {
                IPEndPoint = new IPEndPoint(address, 0);
                SocketAddress = IPEndPoint.Serialize();
            }
        }

        private unsafe void LoadSocketTypeFromHandle(
            SafeSocketHandle handle, out AddressFamily addressFamily, out SocketType socketType, out ProtocolType protocolType, out bool blocking, out bool isListening, out bool isSocket)
        {
            Interop.Winsock.EnsureInitialized();
            Interop.Winsock.WSAPROTOCOL_INFOW info = default;
            int optionLength = sizeof(Interop.Winsock.WSAPROTOCOL_INFOW);
            if (Interop.Winsock.getsockopt(handle, SocketOptionLevel.Socket, (SocketOptionName)Interop.Winsock.SO_PROTOCOL_INFOW, (byte*)&info, ref optionLength) == SocketError.SocketError)
            {
                throw new SocketException((int)SocketPal.GetLastSocketError());
            }

            addressFamily = info.iAddressFamily;
            socketType = info.iSocketType;
            protocolType = info.iProtocol;

            isListening = SocketPal.GetSockOpt(_handle, SocketOptionLevel.Socket,
                SocketOptionName.AcceptConnection, 
                out int isListeningValue) == SocketError.Success && isListeningValue != 0;
            blocking = true;
            isSocket = true;
        }

        private void EnableReuseUnicastPort()
        {
            int optionValue = 1;
            SocketError error = Interop.Winsock.setsockopt(
                _handle,
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseUnicastPort,
                ref optionValue,
                sizeof(int));
        }

        internal static void SocketListToFileDescriptorSet(IList? socketList, Span<IntPtr> fileDescriptorSet, ref int refsAdded)
        {
            int count;
            if (socketList == null || (count = socketList.Count) == 0)
            {
                return;
            }

            Debug.Assert(fileDescriptorSet.Length >= count + 1);

            fileDescriptorSet[0] = (IntPtr)count;
            for (int current = 0; current < count; current++)
            {
                if (!(socketList[current] is Socket socket))
                {
                    throw new ArgumentException();
                }

                bool success = false;
                socket.InternalSafeHandle.DangerousAddRef(ref success);
                fileDescriptorSet[current + 1] = socket.InternalSafeHandle.DangerousGetHandle();
                refsAdded++;
            }
        }
        
        internal static void SelectFileDescriptor(IList? socketList, Span<IntPtr> fileDescriptorSet, ref int refsAdded)
        {
            int count;
            if (socketList == null || (count = socketList.Count) == 0)
            {
                return;
            }

            Debug.Assert(fileDescriptorSet.Length >= count + 1);
            int returnedCount = (int)fileDescriptorSet[0];
            if (returnedCount == 0)
            {
                SocketListDangerousReleaseRefs(socketList, ref refsAdded);
                socketList.Clear();
                return;
            }

            lock (socketList)
            {
                for (int currentSocket = 0; currentSocket < count; currentSocket++)
                {
                    Socket? socket = socketList[currentSocket] as Socket;
                    Debug.Assert(socket != null);

                    int currentFileDescriptor;
                    for (currentFileDescriptor = 0; currentFileDescriptor < returnedCount; currentFileDescriptor++)
                    {
                        if (fileDescriptorSet[currentFileDescriptor + 1] == socket._handle.DangerousGetHandle())
                        {
                            break;
                        }
                    }

                    if (currentFileDescriptor == returnedCount)
                    {
                        socket.InternalSafeHandle.DangerousRelease();
                        refsAdded--;
                        socketList.RemoveAt(currentSocket--);
                        count--;
                    }
                }
            }
        }

        internal ThreadPoolBoundHandle GetOrAllocateThreadPoolBoundHandle() =>
            _handle.GetThreadPoolBoundHandle() ??
            GetOrAllocateThreadPoolBoundHandleSlow();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal ThreadPoolBoundHandle GetOrAllocateThreadPoolBoundHandleSlow()
        {
            bool trySkipCompletionPortOnSuccess = !(CompletionPortHelper.PlatformHasUdpIssue && _protocolType == ProtocolType.Udp);
            return _handle.GetOrAllocateThreadPoolBoundHandle(trySkipCompletionPortOnSuccess);
        }
    }
}

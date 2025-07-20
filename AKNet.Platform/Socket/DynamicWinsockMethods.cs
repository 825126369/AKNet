using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static AKNet.Platform.Interop.Kernel32;

namespace AKNet.Platform.Socket
{
    internal sealed class DynamicWinsockMethods
    {
        private static readonly List<DynamicWinsockMethods> s_methodTable = new List<DynamicWinsockMethods>();

        public static DynamicWinsockMethods GetMethods(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            lock (s_methodTable)
            {
                DynamicWinsockMethods methods;

                for (int i = 0; i < s_methodTable.Count; i++)
                {
                    methods = s_methodTable[i];
                    if (methods._addressFamily == addressFamily && methods._socketType == socketType && methods._protocolType == protocolType)
                    {
                        return methods;
                    }
                }

                methods = new DynamicWinsockMethods(addressFamily, socketType, protocolType);
                s_methodTable.Add(methods);
                return methods;
            }
        }

        private readonly AddressFamily _addressFamily;
        private readonly SocketType _socketType;
        private readonly ProtocolType _protocolType;
        private WSARecvMsgDelegate? _recvMsg;

        private DynamicWinsockMethods(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            _addressFamily = addressFamily;
            _socketType = socketType;
            _protocolType = protocolType;
        }

        private static T CreateDelegate<T>(Func<IntPtr, T> functionPointerWrapper, [NotNull] ref T? cache, SafeSocketHandle socketHandle, string guidString) where T : Delegate
        {
            Guid guid = new Guid(guidString);
            IntPtr ptr;
            SocketError errorCode;

            unsafe
            {
                errorCode = Interop.Winsock.WSAIoctl(
                   socketHandle,
                   Interop.Winsock.IoctlSocketConstants.SIOGETEXTENSIONFUNCTIONPOINTER,
                   ref guid,
                   sizeof(Guid),
                   out ptr,
                   sizeof(IntPtr),
                   out _,
                   IntPtr.Zero,
                   IntPtr.Zero);
            }

            if (errorCode != SocketError.Success)
            {
                throw new SocketException();
            }

            Interlocked.CompareExchange(ref cache, functionPointerWrapper(ptr), null);
            return cache;
        }

        internal unsafe WSARecvMsgDelegate GetWSARecvMsgDelegate(SafeHandle socketHandle)
        { 
            return _recvMsg ?? 
                CreateDelegate(ptr => new SocketDelegateHelper(ptr).WSARecvMsg, ref _recvMsg, socketHandle, "f689d7c86f1f436b8a53e54fe351c322");
        }

        private readonly struct SocketDelegateHelper
        {
            private readonly IntPtr _target;

            public SocketDelegateHelper(IntPtr target)
            {
                _target = target;
            }

            internal unsafe SocketError WSARecvMsg(SafeHandle socketHandle, IntPtr msg, out int bytesTransferred, OVERLAPPED* overlapped, IntPtr completionRoutine)
            {
                IntPtr __socketHandle_gen_native = default;
                bytesTransferred = default;
                SocketError __retVal;
                bool socketHandle__addRefd = false;
                try
                {
                    socketHandle.DangerousAddRef(ref socketHandle__addRefd);
                    __socketHandle_gen_native = socketHandle.DangerousGetHandle();
                    fixed (int* __bytesTransferred_gen_native = &bytesTransferred)
                    {
                        __retVal = ((delegate* unmanaged<IntPtr, IntPtr, int*, OVERLAPPED*, IntPtr, SocketError>)_target)(__socketHandle_gen_native, msg, __bytesTransferred_gen_native, overlapped, completionRoutine);
                    }
                }
                finally
                {
                    if (socketHandle__addRefd)
                    {
                        socketHandle.DangerousRelease();
                    }
                }

                return __retVal;
            }
        }
    }

    internal unsafe delegate SocketError WSARecvMsgDelegate(
                SafeHandle socketHandle,
                IntPtr msg,
                out int bytesTransferred,
                NativeOverlapped* overlapped,
                IntPtr completionRoutine);
}

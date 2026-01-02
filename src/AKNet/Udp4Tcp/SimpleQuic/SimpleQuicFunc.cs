using AKNet.Common;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AKNet.Udp4Tcp.Common
{
    internal enum ConnectionType
    {
        Client,
        Server,
    }

    internal enum ConnectionAsyncOperation
    {
        None = 0,
        Accept,
        Connect,
        Disconnect,
        Receive,
        Send,
    }

    internal enum ConnectionError
    {
        Success = 1,
        Error = 2,
    }

    internal enum E_LOGIC_RESULT
    {
        Success = 0,
        Error = 1,
    }

    internal static partial class SimpleQuicFunc
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FAILED(E_LOGIC_RESULT Status)
        {
            return Status == E_LOGIC_RESULT.Error;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SUCCESSED(E_LOGIC_RESULT Status)
        {
            return Status == E_LOGIC_RESULT.Success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThreadCheck(Connection mConnection)
        {
            ThreadCheck(mConnection.mLogicWorker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThreadCheck(LogicWorker mLogicWorker)
        {
#if DEBUG
            int nThreadId = Thread.CurrentThread.ManagedThreadId;
            if (nThreadId != mLogicWorker.mThreadWorker.ThreadID)
            {
                NetLog.LogError($"ThreadCheck: {mLogicWorker.mThreadWorker.ThreadID}, {nThreadId}");
            }
#endif
        }
    }
}

using System.Diagnostics;
namespace AKNet.Platform.Socket
{
    internal sealed unsafe partial class IOCompletionCallbackHelper
    {
        private readonly IOCompletionCallback _ioCompletionCallback;
        private readonly ExecutionContext _executionContext;
        private uint _errorCode;
        private uint _numBytes;
        private NativeOverlapped* _pNativeOverlapped;

        public IOCompletionCallbackHelper(IOCompletionCallback ioCompletionCallback, ExecutionContext executionContext)
        {
            _ioCompletionCallback = ioCompletionCallback;
            _executionContext = executionContext;
        }
        
        private static readonly ContextCallback IOCompletionCallback_Context_Delegate = IOCompletionCallback_Context;
        private static void IOCompletionCallback_Context(object? state)
        {
            IOCompletionCallbackHelper helper = (IOCompletionCallbackHelper)state!;
            Debug.Assert(helper != null, "IOCompletionCallbackHelper cannot be null");
            helper._ioCompletionCallback(helper._errorCode, helper._numBytes, helper._pNativeOverlapped);
        }

        public static void PerformSingleIOCompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* pNativeOverlapped)
        {
            //Debug.Assert(pNativeOverlapped != null);

            //Overlapped overlapped = Overlapped.GetOverlappedFromNative(pNativeOverlapped);
            //object? callback = overlapped._callback;
            //if (callback is IOCompletionCallback iocb)
            //{
            //    iocb(errorCode, numBytes, pNativeOverlapped);
            //    return;
            //}

            //if (callback == null)
            //{
            //    return;
            //}
            
            //Debug.Assert(callback is IOCompletionCallbackHelper);
            //var helper = (IOCompletionCallbackHelper)callback;
            //helper._errorCode = errorCode;
            //helper._numBytes = numBytes;
            //helper._pNativeOverlapped = pNativeOverlapped;
            //ExecutionContext.RunInternal(helper._executionContext, IOCompletionCallback_Context_Delegate, helper);
        }
    }
}

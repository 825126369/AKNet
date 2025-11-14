/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:44
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace AKNet.Common.Channel
{
    internal abstract class AsyncOperation
    {
        protected static readonly Action<object?> s_availableSentinel = AvailableSentinel; // named method to help with debugging
        private static void AvailableSentinel(object? s) => Debug.Fail($"{nameof(AsyncOperation)}.{nameof(AvailableSentinel)} invoked with {s}");
        protected static readonly Action<object?> s_completedSentinel = CompletedSentinel; // named method to help with debugging
        private static void CompletedSentinel(object? s) => Debug.Fail($"{nameof(AsyncOperation)}.{nameof(CompletedSentinel)} invoked with {s}");
        protected static void ThrowIncompleteOperationException() => throw new InvalidOperationException();
        protected static void ThrowMultipleContinuations() => throw new InvalidOperationException();
        protected static void ThrowIncorrectCurrentIdException() => throw new InvalidOperationException();
    }
        
    internal partial class AsyncOperation<TResult> : AsyncOperation, IValueTaskSource, IValueTaskSource<TResult>
    {
        private readonly CancellationTokenRegistration _registration;
        private readonly bool _pooled;
        private readonly bool _runContinuationsAsynchronously;
        private volatile int _completionReserved;
        private TResult? _result;
        private ExceptionDispatchInfo? _error;
        private Action<object?>? _continuation;
        private object? _continuationState;
        private object? _schedulingContext;
        private ExecutionContext? _executionContext;
        private short _currentId;
        
        public AsyncOperation(bool runContinuationsAsynchronously, CancellationToken cancellationToken = default, bool pooled = false)
        {
            _continuation = pooled ? s_availableSentinel : null;
            _pooled = pooled;
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
            if (cancellationToken.CanBeCanceled)
            {
                Debug.Assert(!_pooled, "Cancelable operations can't be pooled");
                CancellationToken = cancellationToken;
                _registration = cancellationToken.Register(static s =>
                {
                    var thisRef = (AsyncOperation<TResult>)s!;
                    thisRef.TrySetCanceled(thisRef.CancellationToken);
                }, this);
            }
        }
        
        public AsyncOperation<TResult>? Next { get; set; }
        public CancellationToken CancellationToken { get; }
        public ValueTask ValueTask => new ValueTask(this, _currentId);
        public ValueTask<TResult> ValueTaskOfT => new ValueTask<TResult>(this, _currentId);
        public ValueTaskSourceStatus GetStatus(short token)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            return !IsCompleted ? ValueTaskSourceStatus.Pending :
                _error == null ? ValueTaskSourceStatus.Succeeded :
                _error.SourceException is OperationCanceledException ? ValueTaskSourceStatus.Canceled :
                ValueTaskSourceStatus.Faulted;
        }
            
        internal bool IsCompleted => ReferenceEquals(_continuation, s_completedSentinel);
        public TResult GetResult(short token)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            if (!IsCompleted)
            {
                ThrowIncompleteOperationException();
            }

            ExceptionDispatchInfo? error = _error;
            TResult? result = _result;
            _currentId++;

            if (_pooled)
            {
                Volatile.Write(ref _continuation, s_availableSentinel); // only after fetching all needed data
            }

            error?.Throw();
            return result!;
        }
        
        void IValueTaskSource.GetResult(short token)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            if (!IsCompleted)
            {
                ThrowIncompleteOperationException();
            }

            ExceptionDispatchInfo? error = _error;
            _currentId++;

            if (_pooled)
            {
                Volatile.Write(ref _continuation, s_availableSentinel); // only after fetching all needed data
            }

            error?.Throw();
        }
        
        public bool TryOwnAndReset()
        {
            if (ReferenceEquals(Interlocked.CompareExchange(ref _continuation, null, s_availableSentinel), s_availableSentinel))
            {
                _continuationState = null;
                _result = default;
                _error = null;
                _schedulingContext = null;
                _executionContext = null;
                return true;
            }

            return false;
        }
        
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }
            
            if (_continuationState != null)
            {
                ThrowMultipleContinuations();
            }
            _continuationState = state;

            // Capture the execution context if necessary.
            Debug.Assert(_executionContext == null);
            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                _executionContext = ExecutionContext.Capture();
            }

            // Capture the scheduling context if necessary.
            Debug.Assert(_schedulingContext == null);
            SynchronizationContext? sc = null;
            TaskScheduler? ts = null;
            if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                sc = SynchronizationContext.Current;
                if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                {
                    _schedulingContext = sc;
                }
                else
                {
                    sc = null;
                    ts = TaskScheduler.Current;
                    if (ts != TaskScheduler.Default)
                    {
                        _schedulingContext = ts;
                    }
                }
            }
            
            Action<object?>? prevContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
            if (prevContinuation != null)
            {
                Debug.Assert(IsCompleted, $"Expected IsCompleted");
                if (!ReferenceEquals(prevContinuation, s_completedSentinel))
                {
                    Debug.Assert(prevContinuation != s_availableSentinel, "Continuation was the available sentinel.");
                    ThrowMultipleContinuations();
                }
                
                if (_schedulingContext == null)
                {
                    if (_executionContext == null)
                    {
                        UnsafeQueueUserWorkItem(continuation, state);
                    }
                    else
                    {
                        QueueUserWorkItem(continuation, state);
                    }
                }
                else if (sc != null)
                {
                    sc.Post(static s =>
                    {
                        var t = (KeyValuePair<Action<object?>, object?>)s!;
                        t.Key(t.Value);
                    }, new KeyValuePair<Action<object?>, object?>(continuation, state));
                }
                else
                {
                    Debug.Assert(ts != null);
                    Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                }
            }
        }

        /// <summary>Unregisters from cancellation and returns whether cancellation already started.</summary>
        /// <returns>
        /// true if either the instance wasn't cancelable or cancellation successfully unregistered without cancellation having started.
        /// false if cancellation successfully unregistered after cancellation was initiated.
        /// </returns>
        /// <remarks>
        /// This is important for two reasons:
        /// 1. To avoid leaking a registration into a token, so it must be done prior to completing the operation.
        /// 2. To avoid having to worry about concurrent completion; once invoked, the caller can be guaranteed
        /// that no one else will try to complete the operation (assuming the caller is properly constructed
        /// and themselves guarantees only a single completer other than through cancellation).
        /// </remarks>
        public bool UnregisterCancellation()
        {
            if (CancellationToken.CanBeCanceled)
            {
                _registration.Dispose(); // Dispose rather than Unregister is important to know work has quiesced
                return _completionReserved == 0;
            }

            Debug.Assert(_registration == default);
            return true;
        }

        /// <summary>Completes the operation with a success state and the specified result.</summary>
        /// <param name="item">The result value.</param>
        /// <returns>true if the operation could be successfully transitioned to a completed state; false if it was already completed.</returns>
        public bool TrySetResult(TResult item)
        {
            UnregisterCancellation();

            if (TryReserveCompletionIfCancelable())
            {
                _result = item;
                SignalCompletion();
                return true;
            }

            return false;
        }

        /// <summary>Completes the operation with a failed state and the specified error.</summary>
        /// <param name="exception">The error.</param>
        /// <returns>true if the operation could be successfully transitioned to a completed state; false if it was already completed.</returns>
        public bool TrySetException(Exception exception)
        {
            UnregisterCancellation();

            if (TryReserveCompletionIfCancelable())
            {
                _error = ExceptionDispatchInfo.Capture(exception);
                SignalCompletion();
                return true;
            }

            return false;
        }

        /// <summary>Completes the operation with a failed state and a cancellation error.</summary>
        /// <param name="cancellationToken">The cancellation token that caused the cancellation.</param>
        /// <returns>true if the operation could be successfully transitioned to a completed state; false if it was already completed.</returns>
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            if (TryReserveCompletionIfCancelable())
            {
                _error = ExceptionDispatchInfo.Capture(new OperationCanceledException(cancellationToken));
                SignalCompletion();
                return true;
            }

            return false;
        }

        private bool TryReserveCompletionIfCancelable() => !CancellationToken.CanBeCanceled || Interlocked.CompareExchange(ref _completionReserved, 1, 0) == 0;
        
        private void SignalCompletion()
        {
            if (_continuation != null || Interlocked.CompareExchange(ref _continuation, s_completedSentinel, null) != null)
            {
                Debug.Assert(_continuation != s_completedSentinel, $"The continuation was the completion sentinel.");
                Debug.Assert(_continuation != s_availableSentinel, $"The continuation was the available sentinel.");

                if (_schedulingContext == null)
                {
                    if (_runContinuationsAsynchronously)
                    {
                        UnsafeQueueSetCompletionAndInvokeContinuation();
                        return;
                    }
                }
                else if (_schedulingContext is SynchronizationContext sc)
                {
                    if (_runContinuationsAsynchronously || sc != SynchronizationContext.Current)
                    {
                        sc.Post(static s => ((AsyncOperation<TResult>)s!).SetCompletionAndInvokeContinuation(), this);
                        return;
                    }
                }
                else
                {
                    TaskScheduler ts = (TaskScheduler)_schedulingContext;
                    Debug.Assert(ts != null, "Expected a TaskScheduler");
                    if (_runContinuationsAsynchronously || ts != TaskScheduler.Current)
                    {
                        Task.Factory.StartNew(static s => ((AsyncOperation<TResult>)s!).SetCompletionAndInvokeContinuation(), this,
                            CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                        return;
                    }
                }
                SetCompletionAndInvokeContinuation();
            }
        }

        private void SetCompletionAndInvokeContinuation()
        {
            if (_executionContext == null)
            {
                Action<object?> c = _continuation!;
                _continuation = s_completedSentinel;
                c(_continuationState);
            }
            else
            {
                ExecutionContext.Run(_executionContext, static s =>
                {
                    var thisRef = (AsyncOperation<TResult>)s!;
                    Action<object?> c = thisRef._continuation!;
                    thisRef._continuation = s_completedSentinel;
                    c(thisRef._continuationState);
                }, this);
            }
        }


        ///// <summary>The representation of an asynchronous operation that has a result value and carries additional data with it.</summary>
        ///// <typeparam name="TData">Specifies the type of data being written.</typeparam>
        //internal sealed class VoidAsyncOperationWithData<TData> : AsyncOperation<void>
        //{
        //    /// <summary>Initializes the interactor.</summary>
        //    /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        //    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
        //    /// <param name="pooled">Whether this instance is pooled and reused.</param>
        //    public VoidAsyncOperationWithData(bool runContinuationsAsynchronously, CancellationToken cancellationToken = default, bool pooled = false) :
        //        base(runContinuationsAsynchronously, cancellationToken, pooled)
        //    {
        //    }

        //    /// <summary>The item being written.</summary>
        //    public TData? Item { get; set; }
        //}
        
        private void UnsafeQueueSetCompletionAndInvokeContinuation() =>
            ThreadPool.UnsafeQueueUserWorkItem(static s => ((AsyncOperation<TResult>)s).SetCompletionAndInvokeContinuation(), this);

        private static void UnsafeQueueUserWorkItem(Action<object?> action, object? state) =>
            QueueUserWorkItem(action, state);

        private static void QueueUserWorkItem(Action<object?> action, object? state) =>
            Task.Factory.StartNew(action, state,
                CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        private static CancellationTokenRegistration UnsafeRegister(CancellationToken cancellationToken, Action<object?> action, object? state) =>
            cancellationToken.Register(action, state);
    }
}

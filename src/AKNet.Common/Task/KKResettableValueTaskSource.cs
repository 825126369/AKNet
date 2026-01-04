/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet.LinuxTcp")]
[assembly: InternalsVisibleTo("AKNet.WebSocket")]
namespace AKNet.Common
{
    internal sealed class KKResettableValueTaskSource : IValueTaskSource
    {
        public const int State_None = 1;
        public const int State_Awaiting = 2;
        public const int State_Ready = 3;
        public const int State_Completed = 4;

        private int _state;
        private bool _hasWaiter;
        private ManualResetValueTaskSourceCore<bool> _valueTaskSource;
        private CancellationTokenRegistration _cancellationRegistration;
        private CancellationToken _cancelledToken;
        private Action<object?>? _cancellationAction;
        private FinalTaskSource _finalTaskSource;
        private GCHandle _keepAlive;
        private readonly object lock_obj = new object();

        public KKResettableValueTaskSource()
        {
            _state = State_None;
            _hasWaiter = false;
            _valueTaskSource = new ManualResetValueTaskSourceCore<bool>() { RunContinuationsAsynchronously = true };
            _cancellationRegistration = default;
            _cancelledToken = default;
            _finalTaskSource = new FinalTaskSource();
            _keepAlive = default;
        }
        
        public Action<object?> CancellationAction { set{ _cancellationAction = value; } }
        public bool IsCompleted
        {
            get 
            {
                return Volatile.Read(ref _state) == State_Completed; 
            }
        }

        public bool TryGetValueTask(out ValueTask valueTask, object? keepAlive = null, CancellationToken cancellationToken = default)
        {
            lock (lock_obj)
            {
                if (_state == State_None)
                {
                    if (cancellationToken.CanBeCanceled)
                    {
                        _cancellationRegistration = cancellationToken.Register((obj) =>
                        {
                            (KKResettableValueTaskSource thisRef, object target) = ((KKResettableValueTaskSource, object))obj;
                            lock (thisRef)
                            {
                                thisRef._cancelledToken = cancellationToken;
                            }
                            thisRef._cancellationAction?.Invoke(target);
                        }, (this, keepAlive));
                    }
                }

                int state = _state;
                if (_state == State_None)
                {
                    if (keepAlive != null)
                    {
                        Debug.Assert(!_keepAlive.IsAllocated);
                        _keepAlive = GCHandle.Alloc(keepAlive);
                    }
                    _state = State_Awaiting;
                }

                if (state == State_None || state == State_Ready || state == State_Completed)
                {
                    _hasWaiter = true;
                    valueTask = new ValueTask(this, _valueTaskSource.Version);
                    return true;
                }

                valueTask = default;
                return false;
            }
        }
        
        public Task GetFinalTask(object? keepAlive)
        {
            lock (lock_obj)
            {
                return _finalTaskSource.GetTask(keepAlive);
            }
        }

        private bool TryComplete(Exception exception, bool final)
        {
            CancellationTokenRegistration cancellationRegistration = default;
            lock (lock_obj)
            {
                cancellationRegistration = _cancellationRegistration;
                _cancellationRegistration = default;
            }
            cancellationRegistration.Dispose();

            lock (lock_obj)
            {
                try
                {
                    int state = _state;
                    if (state == State_Completed)
                    {
                        return false;
                    }

                    if (state == State_Ready && !_hasWaiter && final)
                    {
                        _valueTaskSource.Reset();
                        state = State_None;
                    }

                    if (state == State_None || state == State_Awaiting)
                    {
                        _state = final ? State_Completed : State_Ready;
                    }

                    if (exception != null)
                    {
                        exception = exception.StackTrace == null ? ExceptionDispatchInfo.Capture(exception).SourceException : exception;
                        if (state == State_None || state == State_Awaiting)
                        {
                            _valueTaskSource.SetException(exception);
                        }
                    }
                    else
                    {
                        if (state == State_None || state == State_Awaiting)
                        {
                            _valueTaskSource.SetResult(final);
                        }
                    }
                    if (final)
                    {
                        if (_finalTaskSource.TryComplete(exception))
                        {
                            if (state != State_Ready)
                            {
                                _finalTaskSource.TrySignal(out _);
                            }
                            return true;
                        }
                        return false;
                    }
                    return state != State_Ready;
                }
                finally
                {
                    if (_keepAlive.IsAllocated)
                    {
                        _keepAlive.Free();
                    }
                }
            }
        }
        
        public bool TrySetResult(bool final = false)
        {
            return TryComplete(null, final);
        }

        public bool TrySetException(Exception exception, bool final = false)
        {
            return TryComplete(exception, final);
        }

        ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
        {
            return _valueTaskSource.GetStatus(token);
        }

        void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        { 
            _valueTaskSource.OnCompleted(continuation, state, token, flags);
        }

        void IValueTaskSource.GetResult(short token)
        {
            try
            {
                _cancelledToken.ThrowIfCancellationRequested();
                _valueTaskSource.GetResult(token);
            }
            finally
            {
                lock (lock_obj)
                {
                    int state = _state;

                    _hasWaiter = false;
                    _cancelledToken = default;

                    if (state == State_Ready)
                    {
                        _valueTaskSource.Reset();
                        _state = State_None;
                        if (_finalTaskSource.TrySignal(out Exception exception))
                        {
                            _state = State_Completed;
                            if (exception != null)
                            {
                                _valueTaskSource.SetException(exception);
                            }
                            else
                            {
                                _valueTaskSource.SetResult(true);
                            }
                        }
                        else
                        {
                            _state = State_None;
                        }
                    }
                }
            }
        }
        
        private struct FinalTaskSource
        {
            private TaskCompletionSource<bool> _finalTaskSource;
            private bool _isCompleted;
            private bool _isSignaled;
            private Exception _exception;

            public Task GetTask(object keepAlive)
            {
                if (_finalTaskSource == null)
                {
                    if (_isSignaled)
                    {
                        return _exception == null ? Task.CompletedTask : Task.FromException(_exception);
                    }

                    _finalTaskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    if (!_isCompleted)
                    {
                        GCHandle handle = GCHandle.Alloc(keepAlive);
                        _finalTaskSource.Task.ContinueWith((_, state) =>
                        {
                            ((GCHandle)state!).Free();
                        }, handle, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    }
                }
                return _finalTaskSource.Task;
            }

            public bool TryComplete(Exception exception = null)
            {
                if (_isCompleted)
                {
                    return false;
                }

                _exception = exception;
                _isCompleted = true;
                return true;
            }

            public bool TrySignal(out Exception exception)
            {
                if (!_isCompleted)
                {
                    exception = default;
                    return false;
                }

                if (_exception != null)
                {
                    _finalTaskSource?.SetException(_exception);
                }
                else
                {
                    _finalTaskSource?.SetResult(true);
                }

                exception = _exception;
                _isSignaled = true;
                return true;
            }
        }

    }
}

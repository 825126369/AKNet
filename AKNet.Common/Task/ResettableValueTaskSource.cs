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

[assembly: InternalsVisibleTo("AKNet.MSQuic")]
namespace AKNet.Common
{
    internal sealed class ResettableValueTaskSource : IValueTaskSource
    {
        // None -> [TryGetValueTask] -> Awaiting -> [TrySetResult|TrySetException(final: false)] -> Ready -> [GetResult] -> None
        // None -> [TrySetResult|TrySetException(final: false)] -> Ready -> [TryGetValueTask] -> [GetResult] -> None
        // None|Awaiting -> [TrySetResult|TrySetException(final: true)] -> Completed(never leaves this state)
        // Ready -> [GetResult: TrySet*(final: true) was called] -> Completed(never leaves this state)
        private enum State:byte
        {
            None,
            Awaiting,
            Ready,
            Completed
        }

        private State _state;
        private bool _hasWaiter;
        private ManualResetValueTaskSourceCore<bool> _valueTaskSource;
        private CancellationTokenRegistration _cancellationRegistration;
        private CancellationToken _cancelledToken;
        private Action<object?>? _cancellationAction;
        private FinalTaskSource _finalTaskSource;
        private GCHandle _keepAlive;
        private readonly object lock_obj = new object();

        public ResettableValueTaskSource()
        {
            _state = State.None;
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
                return (State)Volatile.Read(
                    ref MemoryMarshal.GetReference(MemoryMarshal.Cast<State, byte>(MemoryMarshal.CreateSpan(ref _state, 1)))) == State.Completed; 
            }
        }

        public bool TryGetValueTask(out ValueTask valueTask, object? keepAlive = null, CancellationToken cancellationToken = default)
        {
            lock (lock_obj)
            {
                if (_state == State.None)
                {
                    if (cancellationToken.CanBeCanceled)
                    {
                        _cancellationRegistration = cancellationToken.Register((obj) =>
                        {
                            (ResettableValueTaskSource thisRef, object target) = ((ResettableValueTaskSource, object))obj;
                            lock (thisRef)
                            {
                                thisRef._cancelledToken = cancellationToken;
                            }
                            thisRef._cancellationAction?.Invoke(target);
                        }, (this, keepAlive));
                    }
                }

                State state = _state;
                if (_state == State.None)
                {
                    if (keepAlive != null)
                    {
                        Debug.Assert(!_keepAlive.IsAllocated);
                        _keepAlive = GCHandle.Alloc(keepAlive);
                    }
                    _state = State.Awaiting;
                }

                if (state == State.None || state == State.Ready || state == State.Completed)
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
                    State state = _state;
                    if (state == State.Completed)
                    {
                        return false;
                    }

                    if (state == State.Ready && !_hasWaiter && final)
                    {
                        _valueTaskSource.Reset();
                        state = State.None;
                    }

                    if (state == State.None || state == State.Awaiting)
                    {
                        _state = final ? State.Completed : State.Ready;
                    }

                    if (exception != null)
                    {
                        exception = exception.StackTrace == null ? ExceptionDispatchInfo.Capture(exception).SourceException : exception;
                        if (state == State.None || state == State.Awaiting)
                        {
                            _valueTaskSource.SetException(exception);
                        }
                    }
                    else
                    {
                        if (state == State.None || state == State.Awaiting)
                        {
                            _valueTaskSource.SetResult(final);
                        }
                    }
                    if (final)
                    {
                        if (_finalTaskSource.TryComplete(exception))
                        {
                            if (state != State.Ready)
                            {
                                _finalTaskSource.TrySignal(out _);
                            }
                            return true;
                        }
                        return false;
                    }
                    return state != State.Ready;
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

        //�޷���ֵ�� await �����ȡ�� ʵ��
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
                    State state = _state;

                    _hasWaiter = false;
                    _cancelledToken = default;

                    if (state == State.Ready)
                    {
                        _valueTaskSource.Reset(); //Reset()���ؼ��� ���� IValueTaskSource��ʹ��ɱ��ػ����á�
                        _state = State.None;
                        if (_finalTaskSource.TrySignal(out Exception exception))
                        {
                            _state = State.Completed;
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
                            _state = State.None;
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

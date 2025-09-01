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
    internal sealed class ValueTaskSource : IValueTaskSource
    {
        private enum State : byte
        {
            None,
            Awaiting,
            Completed
        }

        private State _state;
        private ManualResetValueTaskSourceCore<bool> _valueTaskSource;
        private CancellationTokenRegistration _cancellationRegistration;
        private GCHandle _keepAlive;

        public ValueTaskSource()
        {
            _state = State.None;
            _valueTaskSource = new ManualResetValueTaskSourceCore<bool>() { RunContinuationsAsynchronously = true };
            _cancellationRegistration = default;
        }
        
        public bool IsCompleted
        {
            get
            {
                return (State)Volatile.Read(
                    ref MemoryMarshal.GetReference(MemoryMarshal.Cast<State, byte>(MemoryMarshal.CreateSpan(ref _state, 1)))) == 
                    State.Completed;
            }
        }

        public bool IsCompletedSuccessfully => IsCompleted && _valueTaskSource.GetStatus(_valueTaskSource.Version) == ValueTaskSourceStatus.Succeeded;

        public bool TryInitialize(out ValueTask valueTask, object? keepAlive = null, CancellationToken cancellationToken = default)
        {
            lock (this)
            {
                valueTask = new ValueTask(this, _valueTaskSource.Version);
                if (_state == State.None)
                {
                    if (cancellationToken.CanBeCanceled)
                    {
                        _cancellationRegistration = cancellationToken.Register((obj) =>
                        {
                            ValueTaskSource thisRef = (ValueTaskSource)obj!;
                            thisRef.TrySetException(new OperationCanceledException(cancellationToken));
                        }, this);
                    }
                }

                if (_state == State.None)
                {
                    if (keepAlive != null)
                    {
                        Debug.Assert(!_keepAlive.IsAllocated);
                        _keepAlive = GCHandle.Alloc(keepAlive);
                    }

                    _state = State.Awaiting;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool TryComplete(Exception? exception)
        {
            CancellationTokenRegistration cancellationRegistration = default;
            try
            {
                lock (this)
                {
                    try
                    {
                        State state = _state;
                        if (state != State.Completed)
                        {
                            _state = State.Completed;
                            cancellationRegistration = _cancellationRegistration;
                            _cancellationRegistration = default;

                            if (exception != null)
                            {
                                exception = exception.StackTrace != null ? ExceptionDispatchInfo.Capture(exception).SourceException : exception;
                                _valueTaskSource.SetException(exception);
                            }
                            else
                            {
                                _valueTaskSource.SetResult(true);
                            }

                            return true;
                        }

                        return false;
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
            finally
            {
                cancellationRegistration.Dispose();
            }
        }
        
        public bool TrySetResult()
        {
            return TryComplete(null);
        }
        
        public bool TrySetException(Exception exception)
        {
            return TryComplete(exception);
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
            _valueTaskSource.GetResult(token);
        }

        public void Reset()
        {
            _valueTaskSource.Reset();
        }
    }
}

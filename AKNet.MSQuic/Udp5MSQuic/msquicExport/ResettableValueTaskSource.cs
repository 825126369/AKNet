using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace AKNet.Udp5MSQuic.Common
{
    internal sealed class ResettableValueTaskSource : IValueTaskSource
    {
        // None -> [TryGetValueTask] -> Awaiting -> [TrySetResult|TrySetException(final: false)] -> Ready -> [GetResult] -> None
        // None -> [TrySetResult|TrySetException(final: false)] -> Ready -> [TryGetValueTask] -> [GetResult] -> None
        // None|Awaiting -> [TrySetResult|TrySetException(final: true)] -> Completed(never leaves this state)
        // Ready -> [GetResult: TrySet*(final: true) was called] -> Completed(never leaves this state)
        private enum State
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

        public ResettableValueTaskSource()
        {
            _state = State.None;
            _hasWaiter = false;
            _valueTaskSource = new ManualResetValueTaskSourceCore<bool>() { RunContinuationsAsynchronously = true };
            _cancellationRegistration = default;
            _cancelledToken = default;
            _finalTaskSource = new FinalTaskSource();
        }
        
        public Action<object?> CancellationAction {  set{ _cancellationAction = value; } }
        public bool IsCompleted
        {
            get 
            {
                byte l1 = (byte)_state;
                return (State)Volatile.Read(ref l1) == State.Completed; 
            }
        }

        public bool TryGetValueTask(out ValueTask valueTask, object? keepAlive = null, CancellationToken cancellationToken = default)
        {
            lock (this)
            {
                if (_state == State.None)
                {
                    if (cancellationToken.CanBeCanceled)
                    {
                        _cancellationRegistration = cancellationToken.Register((obj) =>
                        {
                            (ResettableValueTaskSource thisRef, object? target) = ((ResettableValueTaskSource, object?))obj!;
                            lock (thisRef)
                            {
                                thisRef._cancelledToken = cancellationToken;
                            }
                            thisRef._cancellationAction?.Invoke(target);
                        }, (this, keepAlive));
                    }
                }

                State state = _state;
                if (state == State.None)
                {
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
            lock (this)
            {
                return _finalTaskSource.GetTask(keepAlive);
            }
        }

        private bool TryComplete(Exception? exception, bool final)
        {
            CancellationTokenRegistration cancellationRegistration = default;
            lock (this)
            {
                cancellationRegistration = _cancellationRegistration;
                _cancellationRegistration = default;
            }
            cancellationRegistration.Dispose();

            lock (this)
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
                        exception = exception.StackTrace is null ? ExceptionDispatchInfo.Capture(exception).SourceException : exception;
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
                catch (Exception e)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Tries to transition from <see cref="State.Awaiting"/> to either <see cref="State.Ready"/> or <see cref="State.Completed"/>, depending on the value of <paramref name="final"/>.
        /// Only the first call (with either value for <paramref name="final"/>) is able to do that. I.e.: <c>TrySetResult()</c> followed by <c>TrySetResult(true)</c> will both return <c>true</c>.
        /// </summary>
        /// <param name="final">Whether this is the final transition to <see cref="State.Completed" /> or just a transition into <see cref="State.Ready"/> from which the task source can be reset back to <see cref="State.None"/>.</param>
        /// <returns><c>true</c> if this is the first call that set the result; otherwise, <c>false</c>.</returns>
        public bool TrySetResult(bool final = false)
        {
            return TryComplete(null, final);
        }

        /// <summary>
        /// Tries to transition from <see cref="State.Awaiting"/> to either <see cref="State.Ready"/> or <see cref="State.Completed"/>, depending on the value of <paramref name="final"/>.
        /// Only the first call is able to do that with the exception of <c>TrySetResult()</c> followed by <c>TrySetResult(true)</c>, which will both return <c>true</c>.
        /// </summary>
        /// <param name="final">Whether this is the final transition to <see cref="State.Completed" /> or just a transition into <see cref="State.Ready"/> from which the task source can be reset back to <see cref="State.None"/>.</param>
        /// <param name="exception">The exception to set as a result of the value task.</param>
        /// <returns><c>true</c> if this is the first call that set the result; otherwise, <c>false</c>.</returns>
        public bool TrySetException(Exception exception, bool final = false)
        {
            return TryComplete(exception, final);
        }

        ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
            => _valueTaskSource.GetStatus(token);

        void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _valueTaskSource.OnCompleted(continuation, state, token, flags);

        void IValueTaskSource.GetResult(short token)
        {
            try
            {
                _cancelledToken.ThrowIfCancellationRequested();
                _valueTaskSource.GetResult(token);
            }
            finally
            {
                lock (this)
                {
                    State state = _state;

                    _hasWaiter = false;
                    _cancelledToken = default;

                    if (state == State.Ready)
                    {
                        _valueTaskSource.Reset();
                        _state = State.None;

                        // Propagate the _finalTaskSource result into _valueTaskSource if completed.
                        if (_finalTaskSource.TrySignal(out Exception? exception))
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
            private Exception? _exception;

            public Task GetTask(object? keepAlive)
            {
                if (_finalTaskSource == null)
                {
                    if (_isSignaled)
                    {
                        return _exception is null
                            ? Task.CompletedTask
                            : Task.FromException(_exception);
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

            public bool TryComplete(Exception? exception = null)
            {
                if (_isCompleted)
                {
                    return false;
                }

                _exception = exception;
                _isCompleted = true;
                return true;
            }

            public bool TrySignal(out Exception? exception)
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

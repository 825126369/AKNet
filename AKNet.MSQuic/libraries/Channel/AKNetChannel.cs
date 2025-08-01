using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Common.Channel
{
    internal class AKNetChannel<T>
    {
        private readonly TaskCompletionSource<T> _completion;
        private readonly ConcurrentQueue<T> _items = new ConcurrentQueue<T>();
        private readonly Queue<AsyncOperation<T>> _blockedReaders = new Queue<AsyncOperation<T>>();
        private readonly bool _runContinuationsAsynchronously;
        private AsyncOperation<bool>? _waitingReadersTail;
        private Exception? _doneWriting;
        private object SyncObj => _items;
        public UnboundedChannelReader Reader { get; protected set; } = null!;
        public UnboundedChannelWriter Writer { get; protected set; } = null!;

        internal AKNetChannel(bool runContinuationsAsynchronously)
        {
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
            _completion = new TaskCompletionSource<T>(runContinuationsAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);
            Reader = new UnboundedChannelReader(this);
            Writer = new UnboundedChannelWriter(this);
        }

        internal sealed class UnboundedChannelReader
        {
            internal readonly AKNetChannel<T> _parent;
            private readonly AsyncOperation<T> _readerSingleton;
            private readonly AsyncOperation<bool> _waiterSingleton;

            internal UnboundedChannelReader(AKNetChannel<T> parent)
            {
                _parent = parent;
                _readerSingleton = new AsyncOperation<T>(parent._runContinuationsAsynchronously, pooled: true);
                _waiterSingleton = new AsyncOperation<bool>(parent._runContinuationsAsynchronously, pooled: true);
            }

            public Task Completion => _parent._completion.Task;

            public bool CanCount => true;

            public bool CanPeek => true;

            public int Count => _parent._items.Count;

            public ValueTask<T> ReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<T>(Task.FromCanceled<T>(cancellationToken));
                }

                AKNetChannel<T> parent = _parent;
                if (parent._items.TryDequeue(out T? item))
                {
                    CompleteIfDone(parent);
                    return new ValueTask<T>(item);
                }

                lock (parent.SyncObj)
                {
                    if (parent._items.TryDequeue(out item))
                    {
                        CompleteIfDone(parent);
                        return new ValueTask<T>(item);
                    }

                    if (parent._doneWriting != null)
                    {
                        return ChannelUtilities.GetInvalidCompletionValueTask<T>(parent._doneWriting);
                    }
                    
                    if (!cancellationToken.CanBeCanceled)
                    {
                        AsyncOperation<T> singleton = _readerSingleton;
                        if (singleton.TryOwnAndReset())
                        {
                            parent._blockedReaders.Enqueue(singleton);
                            return singleton.ValueTaskOfT;
                        }
                    }

                    var reader = new AsyncOperation<T>(parent._runContinuationsAsynchronously, cancellationToken);
                    parent._blockedReaders.Enqueue(reader);
                    return reader.ValueTaskOfT;
                }
            }

            public bool TryRead([MaybeNullWhen(false)] out T item)
            {
                AKNetChannel<T> parent = _parent;
                if (parent._items.TryDequeue(out item))
                {
                    CompleteIfDone(parent);
                    return true;
                }

                item = default;
                return false;
            }

            public bool TryPeek([MaybeNullWhen(false)] out T item) => _parent._items.TryPeek(out item);

            private static void CompleteIfDone(AKNetChannel<T> parent)
            {
                if (parent._doneWriting != null && parent._items.IsEmpty)
                {
                    ChannelUtilities.Complete(parent._completion, parent._doneWriting);
                }
            }

            public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));
                }

                if (!_parent._items.IsEmpty)
                {
                    return new ValueTask<bool>(true);
                }

                AKNetChannel<T> parent = _parent;
                lock (parent.SyncObj)
                {
                    if (!parent._items.IsEmpty)
                    {
                        return new ValueTask<bool>(true);
                    }
                    
                    if (parent._doneWriting != null)
                    {
                        return parent._doneWriting != ChannelUtilities.s_doneWritingSentinel ?
                            new ValueTask<bool>(Task.FromException<bool>(parent._doneWriting)) :
                            default;
                    }

                    if (!cancellationToken.CanBeCanceled)
                    {
                        AsyncOperation<bool> singleton = _waiterSingleton;
                        if (singleton.TryOwnAndReset())
                        {
                            ChannelUtilities.QueueWaiter(ref parent._waitingReadersTail, singleton);
                            return singleton.ValueTaskOfT;
                        }
                    }

                    var waiter = new AsyncOperation<bool>(parent._runContinuationsAsynchronously, cancellationToken);
                    ChannelUtilities.QueueWaiter(ref parent._waitingReadersTail, waiter);
                    return waiter.ValueTaskOfT;
                }
            }
        }

        internal sealed class UnboundedChannelWriter
        {
            internal readonly AKNetChannel<T> _parent;
            internal UnboundedChannelWriter(AKNetChannel<T> parent) => _parent = parent;

            public bool TryComplete(Exception? error)
            {
                AKNetChannel<T> parent = _parent;
                bool completeTask;

                lock (parent.SyncObj)
                {
                    if (parent._doneWriting != null)
                    {
                        return false;
                    }
                    
                    parent._doneWriting = error ?? ChannelUtilities.s_doneWritingSentinel;
                    completeTask = parent._items.IsEmpty;
                }

                if (completeTask)
                {
                    ChannelUtilities.Complete(parent._completion, error);
                }

                ChannelUtilities.FailOperations<AsyncOperation<T>, T>(parent._blockedReaders, ChannelUtilities.CreateInvalidCompletionException(error));
                ChannelUtilities.WakeUpWaiters(ref parent._waitingReadersTail, result: false, error: error);
                return true;
            }

            public bool TryWrite(T item)
            {
                AKNetChannel<T> parent = _parent;
                while (true)
                {
                    AsyncOperation<T>? blockedReader = null;
                    AsyncOperation<bool>? waitingReadersTail = null;
                    lock (parent.SyncObj)
                    {
                        if (parent._doneWriting != null)
                        {
                            return false;
                        }

                        if (parent._blockedReaders.Count > 0)
                        {
                            parent._items.Enqueue(item);
                            waitingReadersTail = parent._waitingReadersTail;
                            if (waitingReadersTail == null)
                            {
                                return true;
                            }
                            parent._waitingReadersTail = null;
                        }
                        else
                        {
                            blockedReader = parent._blockedReaders.Dequeue();
                        }
                    }

                    if (blockedReader != null)
                    {
                        if (blockedReader.TrySetResult(item))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        ChannelUtilities.WakeUpWaiters(ref waitingReadersTail, result: true);
                        return true;
                    }
                }
            }

            public ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken)
            {
                Exception? doneWriting = _parent._doneWriting;
                return
                    cancellationToken.IsCancellationRequested ? new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken)) :
                    doneWriting == null ? new ValueTask<bool>(true) : // unbounded writing can always be done if we haven't completed
                    doneWriting != ChannelUtilities.s_doneWritingSentinel ? new ValueTask<bool>(Task.FromException<bool>(doneWriting)) :
                    default;
            }

            public ValueTask WriteAsync(T item, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask(Task.FromCanceled(cancellationToken));
                }
                else
                {
                    if (TryWrite(item))
                    {
                        return default;
                    }
                    else
                    {
                        return new ValueTask(Task.FromException(ChannelUtilities.CreateInvalidCompletionException(_parent._doneWriting)));
                    }
                }
            }

        }
    }
}

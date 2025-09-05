using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Common.Channel
{
    internal static class ChannelUtilities
    {
        internal static readonly Exception s_doneWritingSentinel = new Exception(nameof(s_doneWritingSentinel));
        internal static readonly Task<bool> s_trueTask = Task.FromResult(result: true);
        internal static readonly Task<bool> s_falseTask = Task.FromResult(result: false);
        internal static readonly Task s_neverCompletingTask = new TaskCompletionSource<bool>().Task;

        internal static void Complete<T>(TaskCompletionSource<T> tcs, Exception? error = null)
        {
            if (error is OperationCanceledException oce)
            {
                tcs.TrySetCanceled(oce.CancellationToken);
            }
            else if (error != null && error != s_doneWritingSentinel)
            {
                if (tcs.TrySetException(error))
                {
                    _ = tcs.Task.Exception;
                }
            }
            else
            {
                tcs.TrySetResult(default);
            }
        }
        
        internal static ValueTask<T> GetInvalidCompletionValueTask<T>(Exception error)
        {
            Debug.Assert(error != null);

            Task<T> t = error == s_doneWritingSentinel ? Task.FromException<T>(CreateInvalidCompletionException()) :
                error is OperationCanceledException oce ? Task.FromCanceled<T>(oce.CancellationToken.IsCancellationRequested ? oce.CancellationToken : new CancellationToken(true)) :
                Task.FromException<T>(CreateInvalidCompletionException(error));

            return new ValueTask<T>(t);
        }

        internal static void QueueWaiter(ref AsyncOperation<bool>? tail, AsyncOperation<bool> waiter)
        {
            AsyncOperation<bool>? c = tail;
            if (c == null)
            {
                waiter.Next = waiter;
            }
            else
            {
                waiter.Next = c.Next;
                c.Next = waiter;
            }
            tail = waiter;
        }

        internal static void WakeUpWaiters(ref AsyncOperation<bool>? listTail, bool result, Exception? error = null)
        {
            AsyncOperation<bool>? tail = listTail;
            if (tail != null)
            {
                listTail = null;

                AsyncOperation<bool> head = tail.Next!;
                AsyncOperation<bool> c = head;
                do
                {
                    AsyncOperation<bool> next = c.Next!;
                    c.Next = null;

                    bool completed = error != null ? c.TrySetException(error) : c.TrySetResult(result);
                    Debug.Assert(completed || c.CancellationToken.CanBeCanceled);

                    c = next;
                }
                while (c != head);
            }
        }

        /// <summary>Removes all operations from the queue, failing each.</summary>
        /// <param name="operations">The queue of operations to complete.</param>
        /// <param name="error">The error with which to complete each operations.</param>
        internal static void FailOperations<T, TInner>(Queue<T> operations, Exception error) where T : AsyncOperation<TInner>
        {
            Debug.Assert(error != null);
            while (operations.Count > 0)
            {
                operations.Dequeue().TrySetException(error);
            }
        }

        /// <summary>Creates and returns an exception object to indicate that a channel has been closed.</summary>
        internal static Exception CreateInvalidCompletionException(Exception? inner = null) =>
            inner is OperationCanceledException ? inner :
            inner != null && inner != s_doneWritingSentinel ? new ChannelClosedException(inner) :
            new ChannelClosedException();
    }
}

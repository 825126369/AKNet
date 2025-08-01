using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Udp2MSQuic.Common
{
    class ConcurrentQueueAsync<T>
    {
        readonly TaskCompletionSource<T> read_tcs = new TaskCompletionSource<T>();
        readonly ConcurrentQueue<T> mQueue = new ConcurrentQueue<T>();

        public ValueTask<T> ReadAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                read_tcs.SetCanceled();
                return new ValueTask<T>(read_tcs.Task);
            }

            if (mQueue.TryDequeue(out T t))
            {
                read_tcs.SetResult(t);
            }
                
            return new ValueTask<T>(read_tcs.Task);
        }

        public bool TryDequeue(out T t)
        {
            return mQueue.TryDequeue(out t);
        }

        public void Enqueue(T t)
        {
            mQueue.Enqueue(t);
            read_tcs.SetResult(t);
        }
    }
}

/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
#if NETSTANDARD
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Common.Channel
{
    internal static class TaskToAsyncResult
    {
        public static IAsyncResult Begin(Task task, AsyncCallback? callback, object? state)
        {
            return new TaskAsyncResult(task, state, callback);
        }

        public static void End(IAsyncResult asyncResult) => Unwrap(asyncResult).GetAwaiter().GetResult();
        public static TResult End<TResult>(IAsyncResult asyncResult) => Unwrap<TResult>(asyncResult).GetAwaiter().GetResult();
        public static Task Unwrap(IAsyncResult asyncResult)
        {
            if (asyncResult is null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            if ((asyncResult as TaskAsyncResult)?._task is not Task task)
            {
                throw new ArgumentException(null, nameof(asyncResult));
            }

            return task;
        }

        public static Task<TResult> Unwrap<TResult>(IAsyncResult asyncResult)
        {
            if (asyncResult is null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            if ((asyncResult as TaskAsyncResult)?._task is not Task<TResult> task)
            {
                throw new ArgumentException(null, nameof(asyncResult));
            }

            return task;
        }

        private sealed class TaskAsyncResult : IAsyncResult
        {
            internal readonly Task _task;
            private readonly AsyncCallback? _callback;
            internal TaskAsyncResult(Task task, object? state, AsyncCallback? callback)
            {
                Debug.Assert(task is not null);

                _task = task;
                AsyncState = state;

                if (task.IsCompleted)
                {
                    CompletedSynchronously = true;
                    callback?.Invoke(this);
                }
                else if (callback is not null)
                {
                    _callback = callback;
                    _task.ConfigureAwait(continueOnCapturedContext: false)
                         .GetAwaiter()
                         .OnCompleted(() => _callback.Invoke(this));
                }
            }

            public object? AsyncState { get; }
            public bool CompletedSynchronously { get; }
            public bool IsCompleted => _task.IsCompleted;
            public WaitHandle AsyncWaitHandle => ((IAsyncResult)_task).AsyncWaitHandle;

            WaitHandle IAsyncResult.AsyncWaitHandle => ((IAsyncResult)_task).AsyncWaitHandle;
        }
    }
}
#endif

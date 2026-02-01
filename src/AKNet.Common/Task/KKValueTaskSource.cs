/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:46
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
    internal sealed class KKValueTaskSource : IValueTaskSource
    {
        private const int State_None = 1; //初始化一个ValueTask
        private const int State_Awaiting = 2; //等待设置结果
        private const int State_Completed = 3; //设置结果完成
        
        private ManualResetValueTaskSourceCore<bool> _valueTaskSource;
        private CancellationTokenRegistration _cancellationRegistration;
        private GCHandle _keepAlive;
        private readonly object lock_obj = new object();
        private int _state;

        public KKValueTaskSource()
        {
            _state = State_None;
            _valueTaskSource = new ManualResetValueTaskSourceCore<bool>() { RunContinuationsAsynchronously = true };
            _cancellationRegistration = default;
        }
        
        public bool IsCompleted
        {
            get
            {
                return Volatile.Read(ref _state) == State_Completed;
            }
        }

        public bool IsCompletedSuccessfully => IsCompleted && 
            _valueTaskSource.GetStatus(_valueTaskSource.Version) == ValueTaskSourceStatus.Succeeded;

        public bool TryInitialize(out ValueTask valueTask, object? keepAlive = null, CancellationToken cancellationToken = default)
        {
            lock (lock_obj)
            {
                valueTask = new ValueTask(this, _valueTaskSource.Version);
                if (_state == State_None)
                {
                    //获取此标记是否能够处于已取消状态。
                    //CanBeCanceled == false 表示：这个 Token 永远也不会被取消，
                    //因为它关联的是一个 没有取消源的 CancellationToken.None 或者
                    //是一个 已经完成的 CancellationTokenSource
                    if (cancellationToken.CanBeCanceled)
                    {
                        //如果这个调用 cancellationToken 执行了取消操作，就把当前这个 ValueTaskSource 用 OperationCanceledException 设为失败状态。
                        //当取消的时候, 执行这个委托。
                        _cancellationRegistration = cancellationToken.Register((obj) =>
                        {
                            //这个!的意思: 我知道这个表达式可能为 null，但我现在向你保证它不会为 null，请不要再给我 CS8600/CS8602 等 nullable警告。
                            KKValueTaskSource thisRef = (KKValueTaskSource)obj!;
                            thisRef.TrySetException(new OperationCanceledException(cancellationToken));
                        }, this);
                    }
                }

                if (_state == State_None)
                {
                    if (keepAlive != null)
                    {
                        Debug.Assert(!_keepAlive.IsAllocated);
                        _keepAlive = GCHandle.Alloc(keepAlive); //防止被GC
                    }

                    _state = State_Awaiting;
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
                lock (lock_obj)
                {
                    try
                    {
                        int state = _state;
                        if (state != State_Completed)
                        {
                            _state = State_Completed;
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
                cancellationRegistration.Dispose(); //完成之后，把这个注册的委托销毁掉
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

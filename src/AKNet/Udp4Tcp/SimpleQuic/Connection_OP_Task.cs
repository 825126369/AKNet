using AKNet.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Connection
    {
        private bool _disposed;
        private readonly KKResettableValueTaskSource _receiveTcs = new KKResettableValueTaskSource();
        private readonly KKResettableValueTaskSource _sendTcs = new KKResettableValueTaskSource();

        private readonly KKValueTaskSource _connectedTcs = new KKValueTaskSource();
        private readonly KKValueTaskSource _disConnectedTcs = new KKValueTaskSource();

        public void Dispose()
        {
            Volatile.Write(ref m_OnDestroyDontReceiveData, true);
            Volatile.Write(ref m_Connected, false);
            if (mConnectionType == E_CONNECTION_TYPE.Client)
            {
                RemoteEndPoint = null;
                mSocketMgr.Dispose();
                mLogicWorker.RemoveConnection(this);
                mLogicWorker.mThreadWorker.RemoveLogicWorker(mLogicWorker);
                mLogicWorker.mThreadWorker = null;
                mLogicWorker = null;
            }
            else
            {
                mLogicWorker.RemoveConnection(this);
            }
        }

        public async ValueTask ConnectAsync(IPEndPoint targetEndPoint)
        {
            if (m_Connected)
            {
                if (this.RemoteEndPoint == targetEndPoint)
                {
                    
                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                await Task.Delay(1).ConfigureAwait(false); //不要在主线程中做这个
                RemoteEndPoint = targetEndPoint;
                SocketMgr.Config mConfig = new SocketMgr.Config();
                mConfig.bServer = false;
                mConfig.mEndPoint = targetEndPoint;
                mConfig.mReceiveFunc = WorkerThreadReceiveNetPackage;
                this.mConfig = mConfig;

                if(mSocketMgr != null)
                {
                    mSocketMgr.Dispose();
                    mSocketMgr = null;
                }

                mSocketMgr = new SocketMgr();
                if (SimpleQuicFunc.SUCCESSED(mSocketMgr.InitNet(mConfig)))
                {
                    Init(E_CONNECTION_TYPE.Client);
                    mLogicWorker.SetSocketItem(mSocketMgr.GetSocketItem(0));;

                    if (_connectedTcs.TryInitialize(out ValueTask valueTask, this))
                    {

                    }

                    lock (mOPList)
                    {
                        mOPList.AddLast(new ConnectionOP() { nOPType =  E_OP_TYPE.SendConnect });
                    }

                    await valueTask;
                }
                else
                {
                    throw new SocketException();
                }
            }
        }

        public async ValueTask DisconnectAsync()
        {
            if (m_Connected)
            {
                if (_connectedTcs.TryInitialize(out ValueTask valueTask, this))
                {

                }

                lock (mOPList)
                {
                    mOPList.AddLast(new ConnectionOP() { nOPType = E_OP_TYPE.SendDisConnect });
                }

                await valueTask;
            }
        }

        public async ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            //if (_sendTcs.IsCompleted && cancellationToken.IsCancellationRequested)
            //{
            //    throw new TaskCanceledException(this.GetType().Name);
            //}
            
            //if (!_sendTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
            //{
            //    throw new InvalidOperationException(this.GetType().Name);
            //}
            
            //if (buffer.IsEmpty)
            //{
            //    _sendTcs.TrySetResult();
            //    return buffer.Length;
            //}
            
            //await Task.Delay(1).ConfigureAwait(false);
            SendTcpStream(buffer.Span);
            //_sendTcs.TrySetResult();
            return buffer.Length;
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (_receiveTcs.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            int totalCopied = 0;

            if (!_receiveTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
            {
                throw new InvalidOperationException("_receiveTcs.TryGetValueTask");
            }
            
            lock (mMTReceiveStreamList)
            {
                totalCopied += mMTReceiveStreamList.WriteTo(buffer.Span);
            }

            if (totalCopied > 0)
            {
                buffer = buffer.Slice(totalCopied);
                _receiveTcs.TrySetResult();
            }

            await valueTask.ConfigureAwait(false);
            if (totalCopied == 0)
            {
                lock (mMTReceiveStreamList)
                {
                    totalCopied += mMTReceiveStreamList.WriteTo(buffer.Span);
                }
            }

            return totalCopied;
        }
    }
}

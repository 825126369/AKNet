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
        private readonly ResettableValueTaskSource _receiveTcs = new ResettableValueTaskSource();
        private readonly ResettableValueTaskSource _sendTcs = new ResettableValueTaskSource();

        private readonly ValueTaskSource _connectedTcs = new ValueTaskSource();
        private readonly ValueTaskSource _disConnectedTcs = new ValueTaskSource();

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
                        mOPList.AddLast(new ConnectionOP() { nOPType =  ConnectionOP.E_OP_TYPE.SendConnect });
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
                    mOPList.AddLast(new ConnectionOP() { nOPType = ConnectionOP.E_OP_TYPE.SendDisConnect });
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

            if (_sendTcs.IsCompleted && cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException(this.GetType().Name);
            }
             
            if (!_sendTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
            {
                throw new InvalidOperationException(this.GetType().Name);
            }
            
            if (buffer.IsEmpty)
            {
                _sendTcs.TrySetResult();
                return buffer.Length;
            }
            
            SendTcpStream(buffer.Span);
            _sendTcs.TrySetResult();
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
            do
            {
                if (!_receiveTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
                {
                    throw new InvalidOperationException("_receiveTcs.TryGetValueTask");
                }
                
                int copied = 0;
                lock(mMTReceiveStreamList)
                {
                    copied = mMTReceiveStreamList.WriteTo(buffer.Span);
                }

                buffer = buffer.Slice(copied);
                totalCopied += copied;

                if (totalCopied > 0)
                {
                    _receiveTcs.TrySetResult();
                }
                
                await valueTask.ConfigureAwait(false);
            } while (!buffer.IsEmpty && totalCopied == 0);

            return totalCopied;
        }
    }
}

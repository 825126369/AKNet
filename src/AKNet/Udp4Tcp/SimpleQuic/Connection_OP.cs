using System;
using System.Threading;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Connection
    {
        public void Dispose()
        {
            RemoteEndPoint = null;
            Volatile.Write(ref m_Connected, false);
            mLogicWorker.RemoveConnection(this);
            if (mConnectionType == ConnectionType.Client)
            {
                mSocketMgr.Dispose();
                mLogicWorker.mThreadWorker.RemoveLogicWorker(mLogicWorker);
                mLogicWorker.mThreadWorker = null;
            }
            else
            {
                mListener.RemoveFakeSocket(this);
                mListener = null;
            }
        }

        public bool ConnectAsync(ConnectionEventArgs arg)
        {
            bool bIOPending = true;
            if (m_Connected)
            {
                bIOPending = false;
                if (RemoteEndPoint == arg.RemoteEndPoint)
                {
                    arg.LastOperation = ConnectionAsyncOperation.Connect;
                    arg.ConnectionError = ConnectionError.Success;
                }
                else
                {
                    arg.LastOperation = ConnectionAsyncOperation.Connect;
                    arg.ConnectionError = ConnectionError.Error;
                }
            }
            else
            {
                RemoteEndPoint = arg.RemoteEndPoint;
                SocketMgr.Config mConfig = new SocketMgr.Config();
                mConfig.bServer = false;
                mConfig.mEndPoint = arg.RemoteEndPoint;
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
                    Init(ConnectionType.Client);
                    mLogicWorker.SetSocketItem(mSocketMgr.GetSocketItem(0));
                    mWRConnectEventArgs.SetTarget(arg);

                    lock (mOPList)
                    {
                        mOPList.AddLast(new ConnectionOP() { nOPType =  ConnectionOP.E_OP_TYPE.SendConnect });
                    }
                }
                else
                {
                    arg.LastOperation = ConnectionAsyncOperation.Connect;
                    arg.ConnectionError = ConnectionError.Error;
                    bIOPending = false;
                }
            }

            return bIOPending;
        }

        public bool DisconnectAsync(ConnectionEventArgs arg)
        {
            bool bIOPending = true;
            if (m_Connected)
            {
                mWRDisConnectEventArgs.SetTarget(arg);
                lock (mOPList)
                {
                    mOPList.AddLast(new ConnectionOP() { nOPType = ConnectionOP.E_OP_TYPE.SendDisConnect });
                }
            }
            else
            {
                arg.LastOperation = ConnectionAsyncOperation.Disconnect;
                arg.ConnectionError = ConnectionError.Success;
                bIOPending = false;
            }
            return bIOPending;
        }

        public bool SendAsync(ConnectionEventArgs arg)
        {
            bool bIOPending = true;
            SendTcpStream(arg);
            return bIOPending;
        }

        public bool ReceiveAsync(ConnectionEventArgs arg)
        {
            bool bIOPending = true;
            lock (mMTReceiveStreamList)
            {
                if (mMTReceiveStreamList.Length > 0)
                {
                    bIOPending = false;
                    arg.Offset = 0;
                    arg.Length = arg.MemoryBuffer.Length;
                    arg.BytesTransferred = mMTReceiveStreamList.WriteTo(arg.GetCanWriteSpan());
                    arg.LastOperation = ConnectionAsyncOperation.Receive;
                    arg.ConnectionError = ConnectionError.Success;
                }
                else
                {
                    mWRReceiveEventArgs.SetTarget(arg);
                }
            }

            return bIOPending;
        }
    }
}

/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

using AKNet.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp4Tcp.Common
{
    internal class SocketItem : IDisposable
    {
        public Socket mSocket;
        public readonly SSocketAsyncEventArgs ReceiveArgs = new SSocketAsyncEventArgs();
        private readonly IPEndPoint mEndPointEmpty = new IPEndPoint(IPAddress.Any, 0);
        public IPEndPoint RemoteEndPoint;
        private SocketMgr.Config mConfig;
        public LogicWorker mLogicWorker;

        public SocketItem(SocketMgr.Config mConfig)
        {
            this.mConfig = mConfig;
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, int.MaxValue);

            ReceiveArgs = AllocSSocketAsyncEventArgs();
            ReceiveArgs.UserToken = this;

            if (mConfig.bServer)
            {
                ReceiveArgs.RemoteEndPoint = mEndPointEmpty;
            }
            else
            {
                ReceiveArgs.RemoteEndPoint = mConfig.mEndPoint;
            }
        }

        public void InitNet()
        {
            RemoteEndPoint = mConfig.mEndPoint as IPEndPoint;
            if (mConfig.bServer)
            {
                mSocket.Bind(RemoteEndPoint);
            }
            else
            {
                mSocket.Connect(RemoteEndPoint);
            }

            StartReceiveFromAsync();
        }

        public void StartReceiveFromAsync()
        {
            if (mSocket != null)
            {
                try
                {
                    bool bIOPending = mSocket.ReceiveFromAsync(ReceiveArgs);
                    if (!bIOPending)
                    {
                        ProcessReceive(null, ReceiveArgs);
                    }
                }
                catch (Exception e)
                {
                    if (mSocket != null)
                    {
                        NetLog.LogException(e);
                    }
                }
            }
        }

        private void ProcessReceive(object sender, SocketAsyncEventArgs e)
        {
            if (Config.bUseSocketAsyncEventArgsTwoComplete)
            {
                SimpleQuicFunc.ThreadCheck(mLogicWorker);
            }

            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                mConfig.mReceiveFunc(e);
            }
            StartReceiveFromAsync();
        }

        public void SendToAsync(SocketAsyncEventArgs e)
        {
            try
            {
                bool bIOPending = this.mSocket.SendToAsync(e);
                if (!bIOPending)
                {
                    ProcessSend(null, e);
                }
            }
            catch (Exception ex)
            {
                NetLog.LogException(ex);
            }
        }

        public bool SendToAsync2(SocketAsyncEventArgs e)
        {
            return this.mSocket.SendToAsync(e);
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (Config.bUseSocketAsyncEventArgsTwoComplete)
            {
                SimpleQuicFunc.ThreadCheck(mLogicWorker);
            }

            if (e.SocketError == SocketError.Success)
            {
                var mPool = e.UserToken as SSocketAsyncEventArgsPool;
                mPool.recycle(e as SSocketAsyncEventArgs);
            }
            else
            {
                NetLog.LogError(e.SocketError);
            }
        }

        void OnIOComplete1(object sender, SocketAsyncEventArgs arg)
        {
            mLogicWorker.Add_SocketAsyncEventArgs(arg as SSocketAsyncEventArgs);
        }
        
        void OnIOComplete2(object sender, SocketAsyncEventArgs arg)
        {
            switch (arg.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    ProcessReceive(null, arg);
                    break;

                case SocketAsyncOperation.SendTo:
                    ProcessSend(null, arg);
                    break;
                default:
                    NetLog.Assert(false, arg.LastOperation);
                    break;
            }
        }
        
        public void Dispose()
        {
            if (mSocket != null)
            {
                Socket mSocket2 = mSocket;
                mSocket = null;

                try
                {
                    mSocket2.Close();
                }
                catch (Exception) { }
            }
        }

        public SSocketAsyncEventArgs AllocSSocketAsyncEventArgs()
        {
            SSocketAsyncEventArgs arg = new SSocketAsyncEventArgs();
            if (Config.bUseSocketAsyncEventArgsTwoComplete)
            {
                arg.Completed += OnIOComplete1;
                arg.Completed2 += OnIOComplete2;
            }
            else
            {
                arg.Completed += OnIOComplete2;
            }
            arg.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            return arg;
        }
    }

}










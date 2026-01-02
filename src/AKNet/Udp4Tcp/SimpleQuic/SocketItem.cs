/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
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
        private static readonly IPEndPoint mEndPointEmpty = new IPEndPoint(IPAddress.Any, 0);
        public IPEndPoint RemoteEndPoint;
        private SocketMgr.Config mConfig;
        public LogicWorker mLogicWorker;

        public SocketItem(SocketMgr.Config mConfig)
        {
            this.mConfig = mConfig;
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, int.MaxValue);
            
            ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            ReceiveArgs.UserToken = this;

            ReceiveArgs.Completed += OnIOComplete1;
            ReceiveArgs.Completed2 += OnIOComplete2;

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
            bool bIOPending = false;
            if (mSocket != null)
            {
                try
                {
                    bIOPending = mSocket.ReceiveFromAsync(ReceiveArgs);
                    if (!bIOPending)
                    {
                        SimpleQuicFunc.ThreadCheck(mLogicWorker);
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
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                mConfig.mReceiveFunc(e);
            }
            StartReceiveFromAsync();
        }

        public bool SendToAsync(SocketAsyncEventArgs e)
        {
            bool bIOPending = false;

            try
            {
                bIOPending = this.mSocket.SendToAsync(e);
                if (!bIOPending)
                {
                    ProcessSend(null, e);
                }
            }
            catch (Exception ex)
            {
                NetLog.LogException(ex);
            }

            return bIOPending;
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var mPackage = e.UserToken as NetUdpSendFixedSizePackage;
                mPackage.mLogicWorker.mThreadWorker.mSendPackagePool.recycle(mPackage);
            }
            else
            {
                NetLog.LogError(e.SocketError);
            }
        }

        void OnIOComplete1(object sender, SocketAsyncEventArgs arg)
        {
            mLogicWorker.mThreadWorker.Add_SocketAsyncEventArgs(arg as SSocketAsyncEventArgs);
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
    }

}










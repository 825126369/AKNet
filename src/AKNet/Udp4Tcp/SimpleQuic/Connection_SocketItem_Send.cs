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
using System.Net.Sockets;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Connection
    {
        readonly AkCircularManySpanBuffer mSendStreamList = new AkCircularManySpanBuffer(Config.nUdpPackageFixedSize);
        bool bSendIOContexUsed = false;
        int nLastSendBytesCount = 0;
        bool bHaveSocketError = false;
        readonly SSocketAsyncEventArgs SendArgs = new SSocketAsyncEventArgs();

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SendNetStream2(e.BytesTransferred);
            }
            else
            {
                bSendIOContexUsed = false;
                DisConnectedWithSocketError(e.SocketError);
            }
        }
        
        private void SendNetStream2(int BytesTransferred = -1)
        {
            if (BytesTransferred >= 0)
            {
                if (BytesTransferred != nLastSendBytesCount)
                {
                    NetLog.LogError("UDP 发生短写");
                }
            }

            var mSendArgSpan = SendArgs.Buffer.AsSpan();
            int nSendBytesCount = 0;
            lock (mSendStreamList)
            {
                nSendBytesCount += mSendStreamList.WriteToMax(mSendArgSpan);
            }

            if (nSendBytesCount > 0)
            {
                nLastSendBytesCount = nSendBytesCount;
                SendArgs.SetBuffer(0, nSendBytesCount);
                StartSendEventArg();
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

        private void StartSendEventArg()
        {
            bool bIOPending = true;
            try
            {
                SendArgs.RemoteEndPoint = RemoteEndPoint;
                bIOPending = mLogicWorker.mSocketItem.SendToAsync2(SendArgs);
            }
            catch (Exception e)
            {
                DisConnectedWithException(e);
            }

            if (!bIOPending)
            {
                ProcessSend(null, SendArgs);
            }
        }

        public void DisConnectedWithNormal()
        {
            NetLog.Log("客户端 正常 断开服务器 ");
            m_Connected = false;
        }

        private void DisConnectedWithException(Exception e)
        {
            NetLog.LogException(e);
            bHaveSocketError = true;
        }

        private void DisConnectedWithSocketError(SocketError e)
        {
            bHaveSocketError = true;
        }
    }
}










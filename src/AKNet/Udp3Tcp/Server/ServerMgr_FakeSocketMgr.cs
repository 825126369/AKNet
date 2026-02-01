/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:51
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using AKNet.Udp3Tcp.Common;

namespace AKNet.Udp3Tcp.Server
{
    internal partial class ServerMgr
    {
        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            IPEndPoint nPeerId = (IPEndPoint)e.RemoteEndPoint;
            FakeSocket mFakeSocket = null;

            lock (mAcceptSocketDic)
            {
                mAcceptSocketDic.TryGetValue(nPeerId, out mFakeSocket);
            }

            if (mFakeSocket == null)
            {
                if (mAcceptSocketDic.Count >= Config.MaxPlayerCount)
                {
#if DEBUG
                    NetLog.Log($"服务器爆满, FakeSocket 总数: {mAcceptSocketDic.Count}");
#endif
                }
                else
                {
                    mFakeSocket = mFakeSocketPool.Pop();
                    mFakeSocket.RemoteEndPoint = nPeerId;
                    MultiThreadingHandleConnectedSocket(mFakeSocket);

                    lock (mAcceptSocketDic)
                    {
                        mAcceptSocketDic.Add(nPeerId, mFakeSocket);
                    }

                    PrintAddFakeSocketMsg(mFakeSocket);
                }
            }

            if (mFakeSocket != null)
            {
                mFakeSocket.MultiThreadingReceiveNetPackage(e);
            }
        }

        public void RemoveFakeSocket(FakeSocket mFakeSocket)
        {
            var peerId = mFakeSocket.RemoteEndPoint;

            lock (mAcceptSocketDic)
            {
                mAcceptSocketDic.Remove(peerId);
            }

            mFakeSocketPool.recycle(mFakeSocket);
            PrintRemoveFakeSocketMsg(mFakeSocket);
        }

        private void PrintAddFakeSocketMsg(FakeSocket mSocket)
        {
#if DEBUG
            var mRemoteEndPoint = mSocket.RemoteEndPoint;
            if (mRemoteEndPoint != null)
            {
                NetLog.Log($"增加FakeSocket: {mRemoteEndPoint}, FakeSocket总数: {mAcceptSocketDic.Count}");
            }
            else
            {
                NetLog.Log($"增加FakeSocket, FakeSocket总数: {mAcceptSocketDic.Count}");
            }
#endif
        }

        private void PrintRemoveFakeSocketMsg(FakeSocket mSocket)
        {
#if DEBUG
            var mRemoteEndPoint = mSocket.RemoteEndPoint;
            if (mRemoteEndPoint != null)
            {
                NetLog.Log($"移除FakeSocket: {mRemoteEndPoint}, FakeSocket总数: {mAcceptSocketDic.Count}");
            }
            else
            {
                NetLog.Log($"移除FakeSocket, FakeSocket总数: {mAcceptSocketDic.Count}");
            }
#endif
        }
    }
}
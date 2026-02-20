/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:50
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp2Tcp.Common;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp2Tcp.Server
{
    internal partial class NetServerMain
    {
        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            IPEndPoint endPoint = (IPEndPoint)e.RemoteEndPoint;
            FakeSocket mFakeSocket = null;
            IPEndPoint nPeerId = endPoint;

            lock (mAcceptSocketDic)
            {
                mAcceptSocketDic.TryGetValue(nPeerId, out mFakeSocket);
            }

            if (mFakeSocket == null)
            {
                if (mAcceptSocketDic.Count >= mConfigInstance.MaxPlayerCount)
                {
#if DEBUG
                    NetLog.Log($"服务器爆满, 客户端总数: {mAcceptSocketDic.Count}");
#endif
                }
                else
                {
                    mFakeSocket = mFakeSocketPool.Pop();
                    mFakeSocket.RemoteEndPoint = endPoint;
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
            IPEndPoint peerId = mFakeSocket.RemoteEndPoint;

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
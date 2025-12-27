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
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Listener
    {
        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            IPEndPoint nPeerId = (IPEndPoint)e.RemoteEndPoint;
            ConnectionPeer mConnectionPeer = null;

            lock (mConnectionPeerDic)
            {
                mConnectionPeerDic.TryGetValue(nPeerId, out mConnectionPeer);
            }

            if (mConnectionPeer == null)
            {
                if (mConnectionPeerDic.Count >= Config.MaxPlayerCount)
                {
#if DEBUG
                    NetLog.Log($"服务器爆满, FakeSocket 总数: {mConnectionPeerDic.Count}");
#endif
                }
                else
                {
                    mConnectionPeer = mConnectionPeerPool.Pop();
                    mConnectionPeer.RemoteEndPoint = nPeerId;
                    lock (mConnectionPeerDic)
                    {
                        mConnectionPeerDic.Add(nPeerId, mConnectionPeer);
                    }

                    PrintAddFakeSocketMsg(mConnectionPeer);
                }
            }

            if (mConnectionPeer != null)
            {
                mConnectionPeer.MultiThreadingReceiveNetPackage(e);
            }
        }

        public void RemoveFakeSocket(ConnectionPeer mConnectionPeer)
        {
            var peerId = mConnectionPeer.RemoteEndPoint;

            lock (mConnectionPeerDic)
            {
                mConnectionPeerDic.Remove(peerId);
            }

            mConnectionPeerPool.recycle(mConnectionPeer);
            PrintRemoveFakeSocketMsg(mConnectionPeer);
        }

        private void PrintAddFakeSocketMsg(ConnectionPeer mSocket)
        {
#if DEBUG
            var mRemoteEndPoint = mSocket.RemoteEndPoint;
            if (mRemoteEndPoint != null)
            {
                NetLog.Log($"增加FakeSocket: {mRemoteEndPoint}, FakeSocket总数: {mConnectionPeerDic.Count}");
            }
            else
            {
                NetLog.Log($"增加FakeSocket, FakeSocket总数: {mConnectionPeerDic.Count}");
            }
#endif
        }

        private void PrintRemoveFakeSocketMsg(ConnectionPeer mSocket)
        {
#if DEBUG
            var mRemoteEndPoint = mSocket.RemoteEndPoint;
            if (mRemoteEndPoint != null)
            {
                NetLog.Log($"移除FakeSocket: {mRemoteEndPoint}, FakeSocket总数: {mConnectionPeerDic.Count}");
            }
            else
            {
                NetLog.Log($"移除FakeSocket, FakeSocket总数: {mConnectionPeerDic.Count}");
            }
#endif
        }
    }
}
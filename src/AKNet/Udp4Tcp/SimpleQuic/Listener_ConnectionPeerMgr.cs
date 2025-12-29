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
            SocketItem mSocketItem = e.UserToken as SocketItem;
            IPEndPoint nPeerId = (IPEndPoint)e.RemoteEndPoint;
            ConnectionPeer mConnectionPeer = null;

            //1: 这里存在一个问题：如果使用多个Socket 同时处理包的话，这里会产生竞争。由于是多个线程竞争，会造成性能瓶颈。
            //2: 暂时这个 SimpleQuic 只考虑1个Socket. 所以这个性能瓶颈暂时不处理了。
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
                    mConnectionPeer = mSocketItem.mThreadWorker.mConnectionPeerPool.Pop();
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
                mConnectionPeer.WorkerThreadReceiveNetPackage(e);
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
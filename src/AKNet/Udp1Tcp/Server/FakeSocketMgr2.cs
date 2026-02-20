/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:48
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp1Tcp.Common;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp1Tcp.Server
{
    internal class FakeSocketMgr2:FakeSocketMgrInterface
    {
        private NetServerMain mNetServer = null;
        private readonly Dictionary<IPEndPoint, FakeSocket> mAcceptSocketDic = null;
        private readonly FakeSocketPool mFakeSocketPool = null;
        private readonly NetUdpFixedSizePackage mInnerCommandCheckPackage = new NetUdpFixedSizePackage();

        public FakeSocketMgr2(NetServerMain mNetServer)
        {
            this.mNetServer = mNetServer;
            mFakeSocketPool = new FakeSocketPool(mNetServer, 0, Config.MaxPlayerCount);
            mAcceptSocketDic = new Dictionary<IPEndPoint, FakeSocket>(Config.MaxPlayerCount);
        }

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
                if (mAcceptSocketDic.Count >= Config.MaxPlayerCount)
                {
#if DEBUG
                    NetLog.Log($"服务器爆满, 客户端总数: {mAcceptSocketDic.Count}");
#endif
                }
                else
                {
                    mFakeSocket = mFakeSocketPool.Pop();
                    mFakeSocket.RemoteEndPoint = endPoint;
                    mNetServer.GetClientPeerMgr2().MultiThreadingHandleConnectedSocket(mFakeSocket);

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
/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class FakeSocketManager3
    {
        private UdpServer mNetServer = null;
        private readonly Dictionary<string, FakeSocket> mAcceptSocketDic = null;
        private readonly FakeSocketPool mFakeSocketPool = null;
        private readonly int nMaxPlayerCount = 0;

        private readonly InnerCommandSendMgr mDisConnectSendMgr = null;
        private readonly InnectCommandPeekPackage mInnerCommandCheckPackage = new InnectCommandPeekPackage();

        public FakeSocketManager3(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
            nMaxPlayerCount = mNetServer.GetConfig().MaxPlayerCount;
            mFakeSocketPool = new FakeSocketPool(mNetServer, nMaxPlayerCount, nMaxPlayerCount);
            mAcceptSocketDic = new Dictionary<string, FakeSocket>(nMaxPlayerCount);
            mDisConnectSendMgr = new InnerCommandSendMgr(mNetServer);
        }

        public bool HaveDisConnectCommand(SocketAsyncEventArgs e)
        {
            var mBuff = e.Buffer.AsSpan().Slice(e.Offset, e.BytesTransferred);
            while (true)
            {
                bool bSucccess = mNetServer.GetCryptoMgr().InnerCommandPeek(mBuff, mInnerCommandCheckPackage);
                if (bSucccess)
                {
                    if (mInnerCommandCheckPackage.nPackageId == UdpNetCommand.COMMAND_DISCONNECT)
                    {
                        return true;
                    }

                    int nReadBytesCount = mInnerCommandCheckPackage.Length;
                    if (mBuff.Length > nReadBytesCount)
                    {
                        mBuff = mBuff.Slice(nReadBytesCount);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    NetLog.LogError("解码失败 !!!");
                    break;
                }
            }
            return false;
        }

        public ReadOnlySpan<byte> SelectConnectCommandStream(SocketAsyncEventArgs e)
        {
            var mBuff = e.Buffer.AsSpan().Slice(e.Offset, e.BytesTransferred);
            while (true)
            {
                bool bSucccess = mNetServer.GetCryptoMgr().InnerCommandPeek(mBuff, mInnerCommandCheckPackage);
                if (bSucccess)
                {
                    if (mInnerCommandCheckPackage.nPackageId == UdpNetCommand.COMMAND_CONNECT)
                    {
                        return mBuff;
                    }

                    int nReadBytesCount = mInnerCommandCheckPackage.Length;
                    if (mBuff.Length > nReadBytesCount)
                    {
                        mBuff = mBuff.Slice(nReadBytesCount);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    NetLog.LogError("解码失败 !!!");
                    break;
                }
            }
            return Span<byte>.Empty;
        }

        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            IPEndPoint endPoint = (IPEndPoint)e.RemoteEndPoint;

            FakeSocket mFakeSocket = null;
            string nPeerId = endPoint.ToString();

            lock (mAcceptSocketDic)
            {
                mAcceptSocketDic.TryGetValue(nPeerId, out mFakeSocket);
            }

            if (mFakeSocket == null)
            {
                if (HaveDisConnectCommand(e))
                {
                    mDisConnectSendMgr.SendInnerNetData(UdpNetCommand.COMMAND_DISCONNECT, endPoint);
                }
                else
                {
                    if (mAcceptSocketDic.Count >= nMaxPlayerCount)
                    {
#if DEBUG
                        NetLog.Log($"服务器爆满, 客户端总数: {mAcceptSocketDic.Count}");
#endif
                    }
                    else
                    {
                        var mCommandSpan = SelectConnectCommandStream(e);
                        if (mCommandSpan != Span<byte>.Empty)
                        {
                            mFakeSocket = mFakeSocketPool.Pop();
                            mFakeSocket.RemoteEndPoint = endPoint;
                            mNetServer.GetClientPeerManager2().MultiThreadingHandleConnectedSocket(mFakeSocket);

                            lock (mAcceptSocketDic)
                            {
                                mAcceptSocketDic.Add(nPeerId, mFakeSocket);
                            }

                            PrintAddFakeSocketMsg(mFakeSocket);
                        }
                    }
                }
            }

            if (mFakeSocket != null)
            {
                mFakeSocket.MultiThreadingReceiveNetPackage(e);
            }
        }

        public void RemoveFakeSocket(FakeSocket mFakeSocket)
        {
            string peerId = mFakeSocket.RemoteEndPoint.ToString();

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
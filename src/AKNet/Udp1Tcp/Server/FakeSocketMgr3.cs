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
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp1Tcp.Server
{
    internal class FakeSocketMgr3: FakeSocketMgrInterface
    {
        private UdpServer mNetServer = null;
        private readonly Dictionary<string, FakeSocket> mAcceptSocketDic = null;
        private readonly FakeSocketPool mFakeSocketPool = null;
        private readonly InnerCommandSendMgr mDisConnectSendMgr = null;
        private readonly InnectCommandPeekPackage mInnerCommandCheckPackage = new InnectCommandPeekPackage();

        public FakeSocketMgr3(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
            mFakeSocketPool = new FakeSocketPool(mNetServer, Config.MaxPlayerCount, Config.MaxPlayerCount);
            mAcceptSocketDic = new Dictionary<string, FakeSocket>(Config.MaxPlayerCount);
            mDisConnectSendMgr = new InnerCommandSendMgr(mNetServer);
        }

        public bool HaveDisConnectCommand(SocketAsyncEventArgs e)
        {
            var mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            while (true)
            {
                bool bSucccess = UdpPackageEncryption.InnerCommandPeek(mBuff, mInnerCommandCheckPackage);
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
            var mBuff = e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred);
            while (true)
            {
                bool bSucccess = UdpPackageEncryption.InnerCommandPeek(mBuff, mInnerCommandCheckPackage);
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
                    var mCommandSpan = SelectConnectCommandStream(e);
                    if (mCommandSpan != Span<byte>.Empty)
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
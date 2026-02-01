/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:49
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
    internal class FakeSocketMgr4:FakeSocketMgrInterface
    {
        private UdpServer mNetServer = null;
        private readonly Dictionary<string, FakeSocket> mAcceptSocketDic = new Dictionary<string, FakeSocket>();
        private readonly FakeSocketPool mFakeSocketPool = null;

        public FakeSocketMgr4(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
            mFakeSocketPool = new FakeSocketPool(mNetServer);
        }

        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            if (Config.bSocketSendMultiPackage)
            {
                var mBuff = e.Buffer.AsSpan().Slice(e.Offset, e.BytesTransferred);
                while (true)
                {
                    var mPackage = mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                    mPackage.remoteEndPoint = e.RemoteEndPoint;
                    bool bSucccess = UdpPackageEncryption.Decode(mBuff, mPackage);
                    if (bSucccess)
                    {
                        int nReadBytesCount = mPackage.Length;
                        MultiThreading_HandleSinglePackage(mPackage);
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
                        mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                        NetLog.LogError("解码失败 !!!");
                        break;
                    }
                }
            }
            else
            {
                NetUdpFixedSizePackage mPackage = mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                Buffer.BlockCopy(e.Buffer, e.Offset, mPackage.buffer, 0, e.BytesTransferred);
                mPackage.Length = e.BytesTransferred;
                mPackage.remoteEndPoint = e.RemoteEndPoint;
                bool bSucccess = UdpPackageEncryption.Decode(mPackage);
                if (bSucccess)
                {
                    MultiThreading_HandleSinglePackage(mPackage);
                }
                else
                {
                    mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                    NetLog.LogError("解码失败 !!!");
                }
            }
        }

        private void MultiThreading_HandleSinglePackage(NetUdpFixedSizePackage mPackage)
        {
            MultiThreading_AddFakeSocket_And_ReceiveNetPackage(mPackage);
        }

        private void MultiThreading_AddFakeSocket_And_ReceiveNetPackage(NetUdpFixedSizePackage mPackage)
        {
            IPEndPoint endPoint = (IPEndPoint)mPackage.remoteEndPoint;
            FakeSocket mFakeSocket = null;

            string nPeerId = endPoint.ToString();

            lock (mAcceptSocketDic)
            {
                if (!mAcceptSocketDic.TryGetValue(nPeerId, out mFakeSocket))
                {
                    if (mPackage.nPackageId == UdpNetCommand.COMMAND_DISCONNECT)
                    {
                        mNetServer.GetInnerCommandSendMgr().SendInnerNetData(UdpNetCommand.COMMAND_DISCONNECT, endPoint);
                    }
                    else if (mPackage.nPackageId == UdpNetCommand.COMMAND_CONNECT)
                    {
                        if (mAcceptSocketDic.Count >= mNetServer.GetConfig().MaxPlayerCount)
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
                            mAcceptSocketDic.Add(nPeerId, mFakeSocket);
                            PrintAddFakeSocketMsg(mFakeSocket);
                        }
                    }
                }
            }

            if (mFakeSocket != null)
            {
                mFakeSocket.MultiThreadingReceiveNetPackage(mPackage);
            }
            else
            {
                mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
            }
        }

        public void RemoveFakeSocket(FakeSocket mFakeSocket)
        {
            string peerId = mFakeSocket.RemoteEndPoint.ToString();
            lock (mAcceptSocketDic)
            {
                mAcceptSocketDic.Remove(peerId);
                mFakeSocketPool.recycle(mFakeSocket);
                PrintRemoveFakeSocketMsg(mFakeSocket);
            }
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
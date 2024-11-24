/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeerSocketMgr
    {
        private UdpServer mNetServer = null;
        private ClientPeer mClientPeer = null;

        readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        readonly ConcurrentQueue<NetUdpFixedSizePackage> mSendPackageQueue = null;
        readonly AkCircularSpanBuffer<byte> mSendStreamList = null;
        bool bSendIOContexUsed = false;

        public ClientPeerSocketMgr(UdpServer mNetServer, ClientPeer mClientPeer)
        {
            this.mNetServer = mNetServer;
            this.mClientPeer = mClientPeer;

            SendArgs.Completed += ProcessSend;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);

            if (Config.bUseSendStream)
            {
                mSendStreamList = new AkCircularSpanBuffer<byte>();
            }
            else
            {
                mSendPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
            }
        }

        public void SetRemoteEndPoint(IPEndPoint mIPEndPoint)
        {
            SendArgs.RemoteEndPoint = mIPEndPoint;
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (Config.bUseSendStream)
                {
                    SendNetStream2();
                }
                else
                {
                    SendNetPackage2();
                }
            }
            else
            {
                NetLog.LogError(e.SocketError);
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
                bSendIOContexUsed = false;
            }
        }

        public void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            MainThreadCheck.Check();
            if (Config.bUseSendAsync)
            {
                if (Config.bUseSendStream)
                {
                    lock (mSendStreamList)
                    {
                        mSendStreamList.WriteFrom(mPackage.GetBufferSpan());
                    }

                    if (!bSendIOContexUsed)
                    {
                        bSendIOContexUsed = true;
                        SendNetStream2();
                    }
                }
                else
                {
                    mSendPackageQueue.Enqueue(mPackage);
                    if (!bSendIOContexUsed)
                    {
                        bSendIOContexUsed = true;
                        SendNetPackage2();
                    }
                }
            }
            else
            {
                mNetServer.GetSocketMgr().SendNetPackage(mPackage);
                if (!Config.bUseSendStream)
                {
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }
        }

        private void SendNetPackage2()
        {
            NetUdpFixedSizePackage mPackage = null;
            if (mSendPackageQueue.TryDequeue(out mPackage))
            {
                int nSendBytesCount = 0;
                Buffer.BlockCopy(mPackage.buffer, 0, SendArgs.Buffer, nSendBytesCount, mPackage.Length);
                nSendBytesCount += mPackage.Length;
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);

                if (Config.bSocketSendMultiPackage)
                {
                    while (mSendPackageQueue.TryPeek(out mPackage))
                    {
                        if (mPackage.Length + nSendBytesCount <= SendArgs.Buffer.Length)
                        {
                            if (mSendPackageQueue.TryDequeue(out mPackage))
                            {
                                Buffer.BlockCopy(mPackage.buffer, 0, SendArgs.Buffer, nSendBytesCount, mPackage.Length);
                                nSendBytesCount += mPackage.Length;
                                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                SendArgs.SetBuffer(0, nSendBytesCount);
                mNetServer.GetSocketMgr().SendNetPackage(SendArgs, ProcessSend);
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

        public void Reset()
        {
            if (Config.bUseSendStream)
            {
                lock (mSendStreamList)
                {
                    mSendStreamList.reset();
                }
            }
            else
            {
                NetUdpFixedSizePackage mPackage = null;
                while (mSendPackageQueue.TryDequeue(out mPackage))
                {
                    mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }
        }

        private void SendNetStream2()
        {
            int CurrentSegmentLength = mSendStreamList.CurrentSegmentLength;
            if (CurrentSegmentLength > 0)
            {
                int nSendBytesCount = 0;
                if (Config.bSocketSendMultiPackage)
                {
                    while (CurrentSegmentLength > 0)
                    {
                        if (CurrentSegmentLength + nSendBytesCount <= SendArgs.Buffer.Length)
                        {
                            lock (mSendStreamList)
                            {
                                mSendStreamList.WriteTo(SendArgs.Buffer.AsSpan().Slice(nSendBytesCount));
                            }
                            nSendBytesCount += CurrentSegmentLength;
                            CurrentSegmentLength = mSendStreamList.CurrentSegmentLength;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    if (CurrentSegmentLength > 0)
                    {
                        lock (mSendStreamList)
                        {
                            mSendStreamList.WriteTo(SendArgs.Buffer.AsSpan().Slice(nSendBytesCount));
                        }
                        nSendBytesCount += CurrentSegmentLength;
                    }
                }

                SendArgs.SetBuffer(0, nSendBytesCount);
                mNetServer.GetSocketMgr().SendNetPackage(SendArgs, ProcessSend);
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

    }
}

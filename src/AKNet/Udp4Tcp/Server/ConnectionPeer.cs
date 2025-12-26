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
using AKNet.Udp4Tcp.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp4Tcp.Server
{
    internal partial class ConnectionPeer : IPoolItemInterface
    {
        private readonly ServerMgr mNetServer;
        private readonly Queue<NetUdpReceiveFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpReceiveFixedSizePackage>();
        private int nCurrentCheckPackageCount = 0;
        public IPEndPoint RemoteEndPoint;
        public UdpCheckMgr mUdpCheckPool = null;

        private readonly object lock_mSocket_object = new object();
        private readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        private readonly AkCircularManySpanBuffer mSendStreamList = null;
        private bool bSendIOContexUsed = false;
        private int nLastSendBytesCount = 0;

        public ConnectionPeer(ServerMgr mNetServer)
        {
            this.mNetServer = mNetServer;
            mUdpCheckPool = new UdpCheckMgr(this);

            SendArgs.Completed += ProcessSend;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            mSendStreamList = new AkCircularManySpanBuffer(Config.nUdpPackageFixedSize);
        }

        public void Update()
        {
            mUdpCheckPool.Update();
            GetReceiveCheckPackage();
        }

        public void AddTcpStream(ReadOnlySpan<byte> mBuffer)
        {
            mSendStreamList.ad
        }

        public void SendInnerNetData(byte id)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(id));
            NetUdpSendFixedSizePackage mPackage = GetObjectPoolManager().UdpSendPackage_Pop();
            mPackage.SetInnerCommandId(id);
            SendNetPackage(mPackage);
            GetObjectPoolManager().UdpSendPackage_Recycle(mPackage);
        }

        public void SendNetPackage(NetUdpSendFixedSizePackage mPackage)
        {
            bool bCanSendPackage = mPackage.orInnerCommandPackage() ||
                GetSocketState() == SOCKET_PEER_STATE.CONNECTED;

            if (bCanSendPackage)
            {
                UdpStatistical.AddSendPackageCount();
                ResetSendHeartBeatCdTime();
                mUdpCheckPool.SetRequestOrderId(mPackage);
                if (mPackage.orInnerCommandPackage())
                {
                    this.SendUDPPackage(mPackage);
                }
                else
                {
                    UdpStatistical.AddSendCheckPackageCount();
                    this.SendUDPPackage(mPackage);
                }
            }
        }

        public int GetCurrentFrameRemainPackageCount()
        {
            return nCurrentCheckPackageCount;
        }

        private bool GetReceiveCheckPackage()
        {
            NetUdpReceiveFixedSizePackage mPackage = null;
            if (GetReceivePackage(out mPackage))
            {
                UdpStatistical.AddReceivePackageCount();
                NetLog.Assert(mPackage != null, "mPackage == null");
                mUdpCheckPool.ReceiveNetPackage(mPackage);
                return true;
            }
            return false;
        }

        public bool GetReceivePackage(out NetUdpReceiveFixedSizePackage mPackage)
        {
            lock (mWaitCheckPackageQueue)
            {
                if (mWaitCheckPackageQueue.TryDequeue(out mPackage))
                {
                    if (!mPackage.orInnerCommandPackage())
                    {
                        nCurrentCheckPackageCount--;
                    }

                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            MainThreadCheck.Check();
            this.mUdpCheckPool.Reset();
            lock (mWaitCheckPackageQueue)
            {
                while (mWaitCheckPackageQueue.TryDequeue(out var mPackage))
                {
                    mNetServer.GetObjectPoolManager().UdpReceivePackage_Recycle(mPackage);
                }
            }
        }
    }
}

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
using System;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Connection
    {
        public void SendTcpStream(ReadOnlySpan<byte> buffer)
        {
            if (!m_Connected) return;
#if DEBUG
            if (buffer.Length > Config.nMaxDataLength)
            {
                NetLog.LogError("超出允许的最大包尺寸：" + Config.nMaxDataLength);
            }
#endif
            lock (mMTSendStreamList)
            {
                mMTSendStreamList.WriteFrom(buffer);
            }
        }

        public void ReceiveTcpStream(NetUdpReceiveFixedSizePackage mPackage)
        {
            if (!m_Connected) return;

            lock (mMTReceiveStreamList)
            {
                mMTReceiveStreamList.WriteFrom(mPackage.GetTcpBufferSpan());
                if (mWRReceiveEventArgs.TryGetTarget(out ConnectionEventArgs arg)) 
                {
                    arg.Offset = 0;
                    arg.Length = arg.MemoryBuffer.Length;
                    arg.BytesTransferred = mMTReceiveStreamList.WriteTo(arg.GetCanWriteSpan());
                    arg.TriggerEvent();
                }
            }
        }

        public void ThreadUpdate()
        {
            if (!m_Connected) return;

            UdpStatistical.AddSearchCount(this.nSearchCount);
            UdpStatistical.AddFrameCount();

            ReSendPackageMgr_AddPackage();
            if (mWaitCheckSendQueue.Count == 0) return;

            bool bTimeOut = false;
            int nSearchCount = this.nSearchCount;
            foreach (var mPackage in mWaitCheckSendQueue)
            {
                if (mPackage.nSendCount > 0)
                {
                    if (mPackage.orTimeOut())
                    {
                        UdpStatistical.AddReSendCheckPackageCount();
                        SendUDPPackage(mPackage);
                        ArrangeReSendTimeOut(mPackage);
                        mPackage.nSendCount++;
                        bTimeOut = true;
                    }
                }
                else
                {
                    UdpStatistical.AddFirstSendCheckPackageCount();
                    SendUDPPackage(mPackage);
                    ArrangeReSendTimeOut(mPackage);
                    mPackage.mTcpStanardRTOTimer.BeginRtt();
                    mPackage.nSendCount++;
                }

                if (--nSearchCount <= 0)
                {
                    break;
                }
            }

            if (bTimeOut)
            {
                this.nSearchCount = Math.Max(this.nSearchCount / 2 + 1, nMinSearchCount);
            }

        }

        public void SendInnerNetData(byte id)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(id));
            NetUdpSendFixedSizePackage mPackage = mThreadWorker.mSendPackagePool.Pop();
            mPackage.SetInnerCommandId(id);
            SendUDPPackage(mPackage);
            mThreadWorker.mSendPackagePool.recycle(mPackage);
        }

        public void SendUDPPackage(NetUdpSendFixedSizePackage mPackage)
        {
            bool bCanSendPackage = mPackage.orInnerCommandPackage() || m_Connected;
            if (bCanSendPackage)
            {
                UdpStatistical.AddSendPackageCount();
                ResetSendHeartBeatCdTime();
                SetRequestOrderId(mPackage);
                if (mPackage.orInnerCommandPackage())
                {
                    this.SendUDPPackage2(mPackage);
                }
                else
                {
                    UdpStatistical.AddSendCheckPackageCount();
                    this.SendUDPPackage2(mPackage);
                }
            }
        }

        public int GetCurrentFrameRemainPackageCount()
        {
            return nCurrentCheckPackageCount;
        }

        public void Reset()
        {
            MainThreadCheck.Check();

            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            mTcpSlidingWindow.WindowReset();
            foreach (var mRemovePackage in mWaitCheckSendQueue)
            {
                mThreadWorker.mSendPackagePool.recycle(mRemovePackage);
            }
            mWaitCheckSendQueue.Clear();

            while (mCacheReceivePackageList.Count > 0)
            {
                int nRemoveIndex = mCacheReceivePackageList.Count - 1;
                NetUdpReceiveFixedSizePackage mRemovePackage = mCacheReceivePackageList[nRemoveIndex];
                mCacheReceivePackageList.RemoveAt(nRemoveIndex);
                mThreadWorker.mReceivePackagePool.recycle(mRemovePackage);
            }

            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        //private void HandleConnectionEvent(ref QUIC_CONNECTION_EVENT connectionEvent)
        //{
        //    NetLog.Log("Connection Event: " + connectionEvent.Type.ToString());
        //    switch (connectionEvent.Type)
        //    {
        //        case E_CONNECTION_EVENT_TYPE.CONNECTED:
        //            HandleEventConnected(ref connectionEvent.CONNECTED);
        //            break;
        //        case E_CONNECTION_EVENT_TYPE.CLOSED:
        //            HandleEventShutdownInitiatedByTransport(ref connectionEvent.SHUTDOWN_INITIATED_BY_TRANSPORT);
        //            break;
        //        case E_CONNECTION_EVENT_TYPE.DATA_RECEIVED:
        //            HandleEventShutdownInitiatedByPeer(ref connectionEvent.SHUTDOWN_INITIATED_BY_PEER);
        //            break;
        //    }
        //}

    }
}

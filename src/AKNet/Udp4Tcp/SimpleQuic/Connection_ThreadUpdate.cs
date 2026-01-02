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
        public void SendTcpStream(ConnectionEventArgs arg)
        {
            if (!m_Connected) return;
#if DEBUG
            if (arg.Length > Config.nMaxDataLength)
            {
                NetLog.LogError("超出允许的最大包尺寸：" + Config.nMaxDataLength);
            }
#endif
            lock (mWRSendEventArgsQueue)
            {
                mWRSendEventArgsQueue.Enqueue(arg);
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
            ProcessConnectionOP();
            if (m_Connected)
            {
                UdpStatistical.AddSearchCount(this.nSearchCount);
                UdpStatistical.AddFrameCount();

                lock (mWRSendEventArgsQueue)
                {
                    while (mWRSendEventArgsQueue.TryDequeue(out ConnectionEventArgs arg))
                    {
                        ReSendPackageMgr_AddTcpStream(arg.GetCanReadSpan());
                        arg.LastOperation = ConnectionAsyncOperation.Send;
                        arg.ConnectionError = ConnectionError.Success;
                        arg.BytesTransferred = arg.Length;
                        arg.SetBuffer(0, arg.MemoryBuffer.Length);
                        arg.TriggerEvent();
                    }
                }

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
        }

        public void SendInnerNetData(byte id)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(id));
            NetUdpSendFixedSizePackage mPackage = mLogicWorker.mThreadWorker.mSendPackagePool.Pop();
            mPackage.SetInnerCommandId(id);
            SendUDPPackage(mPackage);
            mLogicWorker.mThreadWorker.mSendPackagePool.recycle(mPackage);
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
            SimpleQuicFunc.ThreadCheck(this);

            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            mTcpSlidingWindow.WindowReset();
            foreach (var mRemovePackage in mWaitCheckSendQueue)
            {
                mLogicWorker.mThreadWorker.mSendPackagePool.recycle(mRemovePackage);
            }
            mWaitCheckSendQueue.Clear();

            while (mCacheReceivePackageList.Count > 0)
            {
                int nRemoveIndex = mCacheReceivePackageList.Count - 1;
                NetUdpReceiveFixedSizePackage mRemovePackage = mCacheReceivePackageList[nRemoveIndex];
                mCacheReceivePackageList.RemoveAt(nRemoveIndex);
                mLogicWorker.mThreadWorker.mReceivePackagePool.recycle(mRemovePackage);
            }

            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        void ProcessConnectionOP()
        {
            ConnectionOP Oper = GetNextOP();
            while (Oper != null)
            {
                switch (Oper.nOPType)
                {
                    case ConnectionOP.E_OP_TYPE.SendConnect:
                        this.SendConnect();
                        break;

                    case ConnectionOP.E_OP_TYPE.SendDisConnect:
                        this.SendDisConnect();
                        break;

                    default:
                        NetLog.Assert(false);
                        break;
                }

                Oper = GetNextOP();
            }
        }

        private ConnectionOP GetNextOP()
        {
            ConnectionOP Operation = null;
            if (mOPList.Count > 0)
            {
                lock (mOPList)
                {
                    Operation = mOPList.First.Value;
                    mOPList.RemoveFirst();
                }
            }
            return Operation;
        }

    }
}

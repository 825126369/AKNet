using System;
using System.Collections.Generic;
using System.Threading;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Common
{
    internal class UdpCheckMgr
    {
        internal class CheckPackageInfo_TimeOutGenerator
        {
            double fTime = 0;
            double fInternalTime = 0;
            public void SetInternalTime(double fInternalTime)
            {
                this.fInternalTime = fInternalTime;
                this.Reset();
            }

            public void Reset()
            {
                this.fTime = 0.0;
            }

            public bool orTimeOut(double fElapsed)
            {
                this.fTime += fElapsed;
                if (this.fTime >= fInternalTime)
                {
                    this.Reset();
                    return true;
                }

                return false;
            }
        }

        internal class CheckPackageInfo
        {
            private readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator = new CheckPackageInfo_TimeOutGenerator();
            //重发数量
            private bool bInPlaying = false;
            private NetUdpFixedSizePackage mPackage = null;
            private UdpClientPeerCommonBase mClientPeer;
            private readonly TcpStanardRTOFunc mRTOFuc = new TcpStanardRTOFunc();

            public CheckPackageInfo(UdpClientPeerCommonBase mClientPeer)
            {
                this.mClientPeer = mClientPeer;
            }

            public void Reset()
            {
                this.mPackage = null;
                this.bInPlaying = false;
                this.mTimeOutGenerator.Reset();
            }

            public NetUdpFixedSizePackage GetPackage()
            {
                return mPackage;
            }

            public void DoFinish()
            {
                mRTOFuc.FinishRttSuccess();
                this.Reset();
            }

            public bool orFinish()
            {
                return !bInPlaying;
            }

            public void Do(NetUdpFixedSizePackage mOtherPackage)
            {
                this.mPackage = mOtherPackage;
                this.bInPlaying = true;
                mRTOFuc.BeginRtt();
                DelayedCallFunc();
            }

            private void ArrangeNextSend()
            {
                long nTimeOutTime = mRTOFuc.GetRTOTime();
                double fTimeOutTime = nTimeOutTime / 1000.0;
#if DEBUG
                if (fTimeOutTime >= Config.fReceiveHeartBeatTimeOut)
                {
                    NetLog.Log("重发时间：" + nTimeOutTime);
                }
#endif
                mTimeOutGenerator.SetInternalTime(fTimeOutTime);
            }

            private void SendPackageFunc()
            {
                var mPackage2 = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
                mPackage2.CopyFrom(this.mPackage);
                mPackage2.remoteEndPoint = this.mPackage.remoteEndPoint;
                mClientPeer.SendNetPackage(mPackage2);
            }

            private void DelayedCallFunc()
            {
                if (bInPlaying && mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    SendPackageFunc();
                    ArrangeNextSend();
                }
            }

            public void Update(double elapsed)
            {
                if (bInPlaying && mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    if (mTimeOutGenerator.orTimeOut(elapsed))
                    {
                        DelayedCallFunc();
                    }
                }
            }
        }
        
        private ushort nCurrentWaitSendOrderId;
        private ushort nCurrentWaitReceiveOrderId;

        private readonly CheckPackageInfo mCheckPackageInfo = null;
        private readonly Queue<NetUdpFixedSizePackage> mWaitCheckSendQueue = null;
        private NetCombinePackage mCombinePackage = null;

        private UdpClientPeerCommonBase mClientPeer = null;
        public UdpCheckMgr(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;

            mCheckPackageInfo = new CheckPackageInfo(mClientPeer);
            mWaitCheckSendQueue = new Queue<NetUdpFixedSizePackage>();
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        private void AddSendPackageOrderId()
        {
            nCurrentWaitSendOrderId = OrderIdHelper.AddOrderId(nCurrentWaitSendOrderId);
        }

        private void AddReceivePackageOrderId()
        {
            nCurrentWaitReceiveOrderId = OrderIdHelper.AddOrderId(nCurrentWaitReceiveOrderId);
        }

        private void SendPackageCheckResult(ushort nSureOrderId)
        {
            mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_PACKAGECHECK, nSureOrderId);
        }

        private void ReceiveCheckPackage(ushort nSureOrderId)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                if (!mCheckPackageInfo.orFinish())
                {
                    NetUdpFixedSizePackage mCheckPackage = mCheckPackageInfo.GetPackage();
                    if (mCheckPackage.nOrderId == nSureOrderId)
                    {
                        mCheckPackageInfo.DoFinish();
                        NetUdpFixedSizePackage mPackage2 = mWaitCheckSendQueue.Dequeue();
                        NetLog.Assert(mPackage2 == mCheckPackage);
                        ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage2);

                        NetUdpFixedSizePackage mSendPackage = null;
                        if (mWaitCheckSendQueue.TryPeek(out mSendPackage))
                        {
                            mCheckPackageInfo.Do(mSendPackage);
                        }
                    }
                }
            }
        }

        public void SendLogicPackage(UInt16 id, ReadOnlySpan<byte> buffer)
        {
            if (this.mClientPeer.GetSocketState() != SOCKET_PEER_STATE.CONNECTED) return;

            NetLog.Assert(buffer.Length <= Config.nMsgPackageBufferMaxLength, "超出允许的最大包尺寸：" + Config.nMsgPackageBufferMaxLength);
            NetLog.Assert(UdpNetCommand.orNeedCheck(id));
            if (!buffer.IsEmpty)
            {
                int readBytes = 0;
                int nBeginIndex = 0;

                UInt16 groupCount = 0;
                if (buffer.Length % Config.nUdpPackageFixedBodySize == 0)
                {
                    groupCount = (UInt16)(buffer.Length / Config.nUdpPackageFixedBodySize);
                }
                else
                {
                    groupCount = (UInt16)(buffer.Length / Config.nUdpPackageFixedBodySize + 1);
                }

                while (nBeginIndex < buffer.Length)
                {
                    if (nBeginIndex + Config.nUdpPackageFixedBodySize > buffer.Length)
                    {
                        readBytes = buffer.Length - nBeginIndex;
                    }
                    else
                    {
                        readBytes = Config.nUdpPackageFixedBodySize;
                    }

                    var mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
                    mPackage.nOrderId = (UInt16)nCurrentWaitSendOrderId;
                    mPackage.nGroupCount = groupCount;
                    mPackage.nPackageId = id;
                    mPackage.Length = Config.nUdpPackageFixedHeadSize;
                    mPackage.CopyFromMsgStream(buffer, nBeginIndex, readBytes);

                    groupCount = 0;
                    nBeginIndex += readBytes;

                    NetPackageEncryption.Encryption(mPackage);
                    AddSendPackageOrderId();
                    AddSendCheck(mPackage);
                }
            }
            else
            {
                var mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
                mPackage.nOrderId = (UInt16)nCurrentWaitSendOrderId;
                mPackage.nGroupCount = 1;
                mPackage.nPackageId = id;
                mPackage.Length = Config.nUdpPackageFixedHeadSize;

                NetPackageEncryption.Encryption(mPackage);
                AddSendPackageOrderId();
                AddSendCheck(mPackage);
            }
        }

        private void AddSendCheck(NetUdpFixedSizePackage mPackage)
        {
            NetLog.Assert(mPackage.nOrderId > 0);
            mWaitCheckSendQueue.Enqueue(mPackage);

            if (mWaitCheckSendQueue.Count > Config.nUdpMaxOrderId / 2)
            {
                NetLog.Log("待发送的包太多了： " + mWaitCheckSendQueue.Count);
            }

            if (mCheckPackageInfo.orFinish())
            {
                mCheckPackageInfo.Do(mPackage);
            }
        }

        public void ReceiveNetPackage(NetUdpFixedSizePackage mReceivePackage)
        {
            this.mClientPeer.ReceiveHeartBeat();
            if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_PACKAGECHECK)
            {
                ushort nSureOrderId = mReceivePackage.nOrderId;
                ReceiveCheckPackage(nSureOrderId);
            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_HEARTBEAT)
            {
                
            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_CONNECT)
            {
                this.mClientPeer.ReceiveConnect();
            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_DISCONNECT)
            {
                this.mClientPeer.ReceiveDisConnect();
            }

            if (UdpNetCommand.orInnerCommand(mReceivePackage.nPackageId))
            {
                mReceivePackage.remoteEndPoint = null;
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mReceivePackage);
            }
            else
            {
                if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    SendPackageCheckResult(mReceivePackage.nOrderId);
                    CheckReceivePackageLoss(mReceivePackage);
                }
                else
                {
                    mReceivePackage.remoteEndPoint = null;
                    ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mReceivePackage);
                }
            }
        }

        private void CheckReceivePackageLoss(NetUdpFixedSizePackage mPackage)
        {
            bool bIsMyWaitPackage = true;
            if (mPackage.nOrderId != nCurrentWaitReceiveOrderId)
            {
                bIsMyWaitPackage = false;
            }

            if (bIsMyWaitPackage)
            {
                AddReceivePackageOrderId();
                CheckCombinePackage(mPackage);
            }
            else
            {
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
            }
        }

        private void CheckCombinePackage(NetUdpFixedSizePackage mPackage)
        {
            if (mPackage.nGroupCount > 1)
            {
                mCombinePackage = ObjectPoolManager.Instance.mCombinePackagePool.Pop();
                mCombinePackage.Init(mPackage);
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
            }
            else if (mPackage.nGroupCount == 1)
            {
                mClientPeer.AddLogicHandleQueue(mPackage);
            }
            else if (mPackage.nGroupCount == 0)
            {
                if (mCombinePackage != null)
                {
                    if (mCombinePackage.Add(mPackage))
                    {
                        if (mCombinePackage.CheckCombineFinish())
                        {
                            mClientPeer.AddLogicHandleQueue(mCombinePackage);
                            mCombinePackage = null;
                        }
                    }
                    else
                    {
                        //残包
                        NetLog.Assert(false, "残包");
                    }
                }
                else
                {
                    //残包
                    NetLog.Assert(false, "残包");
                }
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
            }
            else
            {
                NetLog.Assert(false);
            }
        }

        public void Update(double elapsed)
        {
            mCheckPackageInfo.Update(elapsed);
        }

        public void Reset()
        {
            if (mCombinePackage != null)
            {
                ObjectPoolManager.Instance.mCombinePackagePool.recycle(mCombinePackage);
                mCombinePackage = null;
            }

            while (mWaitCheckSendQueue.Count > 0)
            {
                NetUdpFixedSizePackage mRemovePackage = mWaitCheckSendQueue.Dequeue();
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mRemovePackage);
            }

            mCheckPackageInfo.Reset();

            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
        }

        public void Release()
        {

        }
    }
}
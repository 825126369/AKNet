using System;
using System.Collections.Generic;
using UdpPointtopointProtocols;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    // 用 并发 Queue 实现 
    internal class UdpCheck3Pool
    {
        internal class CheckPackageInfo
        {
            public NetUdpFixedSizePackage mPackage;
            public Timer mTimer;

            public CheckPackageInfo()
            {
                mTimer = new Timer();
            }

            public bool orTimeOut()
            {
                return mTimer.elapsed() > fReSendTimeOut;
            }
        }

        private const double fReSendTimeOut = 1.0;
        private ushort nCurrentWaitReceiveOrderId;
        private ushort nCurrentWaitSendOrderId;

        private Queue<CheckPackageInfo> mWaitCheckSendQueue = null;
        private Queue<NetCombinePackage> mCombinePackageQueue = null;
        private ushort nWaitSureOrderId = 0;

        private ClientPeer mUdpPeer = null;

        private object lock_nCurrentWaitReceiveOrderId_Obj = new object();
        private object lock_nCurrentWaitSendOrderId_Obj = new object();

        public UdpCheck3Pool(ClientPeer mUdpPeer)
        {
            mWaitCheckSendQueue = new Queue<CheckPackageInfo>();
            mCombinePackageQueue = new Queue<NetCombinePackage>();

            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;

            this.mUdpPeer = mUdpPeer;
        }

        private void AddSendPackageOrderId()
        {
            lock (lock_nCurrentWaitSendOrderId_Obj)
            {
                nCurrentWaitSendOrderId++;
                if (nCurrentWaitSendOrderId > Config.nUdpMaxOrderId)
                {
                    nCurrentWaitSendOrderId = 1;
                }
            }
        }

        private void AddReceivePackageOrderId()
        {
            lock (lock_nCurrentWaitReceiveOrderId_Obj)
            {
                nCurrentWaitReceiveOrderId++;
                if (nCurrentWaitReceiveOrderId > Config.nUdpMaxOrderId)
                {
                    nCurrentWaitReceiveOrderId = 1;
                }
            }
        }

        private void SendPackageCheckResult(uint nSureOrderId, uint nLossOrderId = 0)
        {
            PackageCheckResult mResult = IMessagePool<PackageCheckResult>.Pop();
            mResult.NSureOrderId = nSureOrderId;
            mResult.NLossOrderId = nLossOrderId;
            NetUdpFixedSizePackage mPackage = mUdpPeer.GetUdpSystemPackage(UdpNetCommand.COMMAND_PACKAGECHECK, mResult);
            mUdpPeer.SendNetPackage(mPackage);
            IMessagePool<PackageCheckResult>.recycle(mResult);
        }

        private void ReceiveCheckPackage(NetPackage mPackage)
        {
            NetLog.Assert(mPackage.nPackageId == UdpNetCommand.COMMAND_PACKAGECHECK);

            PackageCheckResult mPackageCheckResult = Protocol3Utility.getData<PackageCheckResult>(mPackage);
            ushort nSureOrderId = (ushort)mPackageCheckResult.NSureOrderId;
            ushort nLossOrderId = (ushort)mPackageCheckResult.NLossOrderId;
            mPackageCheckResult.NSureOrderId = 0;
            mPackageCheckResult.NLossOrderId = 0;
            IMessagePool<PackageCheckResult>.recycle(mPackageCheckResult);

            if (nSureOrderId != nWaitSureOrderId)
            {
                return;
            }

            lock (mWaitCheckSendQueue)
            {
                CheckPackageInfo mRemovePackage = mWaitCheckSendQueue.Dequeue();
                NetLog.Assert(mRemovePackage.mPackage.nOrderId == nWaitSureOrderId);

                NetUdpFixedSizePackage mRecyclePackage = mRemovePackage.mPackage;
                mRemovePackage.mPackage = null;
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mRecyclePackage);
                ObjectPoolManager.Instance.mCheckPackagePool.recycle(mRemovePackage);

                if (mWaitCheckSendQueue.Count > 0)
                {
                    CheckPackageInfo mPeePackage = mWaitCheckSendQueue.Peek();
                    nWaitSureOrderId = mPeePackage.mPackage.nOrderId;
                    mPeePackage.mTimer.restart();
                    this.mUdpPeer.SendNetPackage(mPeePackage.mPackage);
                }
                else
                {
                    nWaitSureOrderId = 0;
                }
            }
        }

        public void SendLogicPackage(UInt16 id, Span<byte> buffer)
        {
            NetLog.Assert(buffer.Length <= Config.nUdpCombinePackageFixedSize);
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
                    mPackage.Length = readBytes + Config.nUdpPackageFixedHeadSize;

                    for (int i = 0; i < readBytes; i++)
                    {
                        mPackage.buffer[Config.nUdpPackageFixedHeadSize + i] = buffer[nBeginIndex + i];
                    }

                    groupCount = 1;
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
                mPackage.nGroupCount = 0;
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

            lock (mWaitCheckSendQueue)
            {
                CheckPackageInfo mCheckInfo = ObjectPoolManager.Instance.mCheckPackagePool.Pop();
                mCheckInfo.mPackage = mPackage;
                mCheckInfo.mTimer.restart();
                mWaitCheckSendQueue.Enqueue(mCheckInfo);

                if (nWaitSureOrderId == 0)
                {
                    nWaitSureOrderId = mPackage.nOrderId;
                    mCheckInfo.mTimer.restart();
                    mUdpPeer.SendNetPackage(mPackage);
                }
            }
        }

        public void ReceivePackage(NetUdpFixedSizePackage mReceivePackage)
        {
            this.mUdpPeer.mUDPLikeTCPMgr.ReceiveHeartBeat();

            if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_PACKAGECHECK)
            {
                ReceiveCheckPackage(mReceivePackage);
            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_CONNECT)
            {
                this.mUdpPeer.mUDPLikeTCPMgr.ReceiveConnect();
            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_DISCONNECT)
            {
                this.mUdpPeer.mUDPLikeTCPMgr.ReceiveDisConnect();
            }

            if (UdpNetCommand.orNeedCheck(mReceivePackage.nPackageId))
            {
                CheckReceivePackageLoss(mReceivePackage);
            }
            else
            {
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mReceivePackage);
            }
        }

        private void CheckReceivePackageLoss(NetUdpFixedSizePackage mPackage)
        {
            SendPackageCheckResult(mPackage.nOrderId);
            if (mPackage.nOrderId != nCurrentWaitReceiveOrderId)
            {
                NetLog.Log("Server 等包: " + mPackage.nPackageId + " | " + mPackage.nOrderId + " | " + mPackage.nGroupCount + " | " + nCurrentWaitReceiveOrderId);
                return;
            }
            
            CheckCombinePackage(mPackage);
            AddReceivePackageOrderId();
        }

        private void CheckCombinePackage(NetUdpFixedSizePackage mPackage)
        {
            lock (mCombinePackageQueue)
            {
                if (mPackage.nGroupCount > 1)
                {
                    NetCombinePackage cc = ObjectPoolManager.Instance.mCombinePackagePool.Pop();
                    cc.Init(mPackage);
                    mCombinePackageQueue.Enqueue(cc);
                }
                else
                {
                    if (mCombinePackageQueue.Count > 0)
                    {
                        NetCombinePackage currentGroup = mCombinePackageQueue.Peek();
                        currentGroup.Add(mPackage);
                        if (currentGroup.CheckCombineFinish())
                        {
                            currentGroup = mCombinePackageQueue.Dequeue();
                            mUdpPeer.mMsgReceiveMgr.AddLogicHandleQueue(currentGroup);
                        }
                    }
                    else
                    {
                        mUdpPeer.mMsgReceiveMgr.AddLogicHandleQueue(mPackage);
                    }
                }
            }
        }

        public void Update(double elapsed)
        {
            if (mUdpPeer.GetSocketState() == SERVER_SOCKET_PEER_STATE.CONNECTED)
            {
                lock (mWaitCheckSendQueue)
                {
                    if (mWaitCheckSendQueue.Count > 0)
                    {
                        CheckPackageInfo mSendPackageInfo = mWaitCheckSendQueue.Peek();
                        if (mSendPackageInfo.mPackage.nOrderId == nWaitSureOrderId)
                        {
                            if (mSendPackageInfo.orTimeOut())
                            {
                                mSendPackageInfo.mTimer.restart();
                                this.mUdpPeer.SendNetPackage(mSendPackageInfo.mPackage);
                            }
                        }
                    }
                }
            }
        }

        public void Reset()
        {
            lock (mWaitCheckSendQueue)
            {
                while (mWaitCheckSendQueue.Count > 0)
                {
                    CheckPackageInfo mRemovePackage = mWaitCheckSendQueue.Dequeue();
                    NetUdpFixedSizePackage mRecyclePackage = mRemovePackage.mPackage;
                    mRemovePackage.mPackage = null;
                    ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mRecyclePackage);
                    ObjectPoolManager.Instance.mCheckPackagePool.recycle(mRemovePackage);
                }
            }

            lock (mCombinePackageQueue)
            {
                while (mCombinePackageQueue.Count > 0)
                {
                    NetCombinePackage mRemovePackage = mCombinePackageQueue.Dequeue();
                    ObjectPoolManager.Instance.mCombinePackagePool.recycle(mRemovePackage);
                }
            }

            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
            nWaitSureOrderId = 0;

        }

        public void Release()
        {
            Reset();
        }
    }
}
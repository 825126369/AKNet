//#define Server

using System;
using System.Collections.Generic;
using UdpPointtopointProtocols;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

#if Server
namespace XKNet.Udp.POINTTOPOINT.Server
#else
namespace XKNet.Udp.POINTTOPOINT.Client
#endif
{
    // 用 并发 Queue 实现 
    internal class UdpCheckMgr
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
        private ushort nLastReceiveOrderId;
        private ushort nCurrentWaitSendOrderId;

        private Queue<CheckPackageInfo> mWaitCheckSendQueue = null;
        private Queue<NetCombinePackage> mCombinePackageQueue = null;
        private ushort nWaitSureOrderId = 0;

        private ClientPeer mClientPeer = null;

        private object lock_nCurrentWaitSendOrderId_Obj = new object();

        public UdpCheckMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;

            mWaitCheckSendQueue = new Queue<CheckPackageInfo>();
            mCombinePackageQueue = new Queue<NetCombinePackage>();

            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nLastReceiveOrderId = 0;
        }

        private ushort AddOrderId(ushort nOrderId)
        {
            nOrderId++;
            if (nOrderId > Config.nUdpMaxOrderId)
            {
                nOrderId = 1;
            }
            return nOrderId;
        }

        private void AddSendPackageOrderId()
        {
            lock (lock_nCurrentWaitSendOrderId_Obj)
            {
                nCurrentWaitSendOrderId = AddOrderId(nCurrentWaitSendOrderId);
            }
        }

        private void SendPackageCheckResult(uint nSureOrderId, uint nLossOrderId = 0)
        {
            PackageCheckResult mResult = IMessagePool<PackageCheckResult>.Pop();
            mResult.NSureOrderId = nSureOrderId;
            mResult.NLossOrderId = nLossOrderId;
            NetUdpFixedSizePackage mPackage = mClientPeer.GetUdpSystemPackage(UdpNetCommand.COMMAND_PACKAGECHECK, mResult);
            mClientPeer.SendNetPackage(mPackage);
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
                    this.mClientPeer.SendNetPackage(mPeePackage.mPackage);
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
                    mClientPeer.SendNetPackage(mPackage);
                }
            }
        }

        public void ReceivePackage(NetUdpFixedSizePackage mReceivePackage)
        {
            this.mClientPeer.mUDPLikeTCPMgr.ReceiveHeartBeat();

            if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_PACKAGECHECK)
            {
                ReceiveCheckPackage(mReceivePackage);
            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_CONNECT)
            {
                this.mClientPeer.mUDPLikeTCPMgr.ReceiveConnect();
            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_DISCONNECT)
            {
                this.mClientPeer.mUDPLikeTCPMgr.ReceiveDisConnect();
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
            if (nLastReceiveOrderId > 0)
            {
                ushort nCurrentWaitReceiveOrderId = AddOrderId(nLastReceiveOrderId);
                if (mPackage.nOrderId != nCurrentWaitReceiveOrderId)
                {
                    NetLog.Log("Server 等包: " + mPackage.nPackageId + " | " + mPackage.nOrderId + " | " + mPackage.nGroupCount + " | " + nCurrentWaitReceiveOrderId);
                    return;
                }
            }
            nLastReceiveOrderId = mPackage.nOrderId;
            CheckCombinePackage(mPackage);
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
                else if (mPackage.nGroupCount == 1)
                {
                    mClientPeer.mMsgReceiveMgr.AddLogicHandleQueue(mPackage);
                }
                else if (mPackage.nGroupCount == 0)
                {
                    if (mCombinePackageQueue.Count > 0)
                    {
                        NetCombinePackage currentGroup = mCombinePackageQueue.Peek();
                        currentGroup.Add(mPackage);
                        ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
                        if (currentGroup.CheckCombineFinish())
                        {
                            currentGroup = mCombinePackageQueue.Dequeue();
                            mClientPeer.mMsgReceiveMgr.AddLogicHandleQueue(currentGroup);
                        }
                    }
                    else
                    {
                        //残包 直接舍弃
                        ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
                    }
                }
                else
                {
                    NetLog.Assert(false);
                }
            }
        }

        public void Update(double elapsed)
        {
#if Server
            if (mClientPeer.GetSocketState() == SERVER_SOCKET_PEER_STATE.CONNECTED)
#else
            if (mClientPeer.GetSocketState() == CLIENT_SOCKET_PEER_STATE.CONNECTED)
#endif
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
                                this.mClientPeer.SendNetPackage(mSendPackageInfo.mPackage);
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
            nWaitSureOrderId = 0;
            nLastReceiveOrderId = 0;
        }

        public void Release()
        {
            Reset();
        }
    }
}
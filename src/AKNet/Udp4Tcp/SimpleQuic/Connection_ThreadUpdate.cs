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
        public void SendTcpStream(ReadOnlySpan<byte> arg)
        {
            if (!m_Connected) return;
#if DEBUG
            if (arg.Length > Config.nMaxDataLength)
            {
                NetLog.LogError("超出允许的最大包尺寸：" + Config.nMaxDataLength);
            }
#endif
            lock (mMTSendStreamList)
            {
                mMTSendStreamList.WriteFrom(arg);
            }
        }

        public void ReceiveTcpStream(NetUdpReceiveFixedSizePackage mPackage)
        {
            if (!m_Connected) return;

            lock (mMTReceiveStreamList)
            {
                mMTReceiveStreamList.WriteFrom(mPackage.GetTcpBufferSpan());
            }
        }

        public void ThreadUpdate()
        {
            ProcessConnectionOP();
            NetCheckPackageExecute();
            mUdpCheckMgr.ThreadUpdate();
        }

        public void SendInnerNetData(byte id)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(id));
            NetUdpSendFixedSizePackage mPackage = mLogicWorker.UdpSendPackage_Pop();
            mPackage.SetInnerCommandId(id);
            SendUDPPackage(mPackage);
            mLogicWorker.UdpSendPackage_Recycle(mPackage);
        }

        public void SendUDPPackage(NetUdpSendFixedSizePackage mPackage)
        {
            bool bCanSendPackage = mPackage.orInnerCommandPackage() || m_Connected;
            if (bCanSendPackage)
            {
                UdpStatistical.AddSendPackageCount();
                ResetSendHeartBeatCdTime();
                mUdpCheckMgr.SetRequestOrderId(mPackage);
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
                         
        void ProcessConnectionOP()
        {
            ConnectionOP Oper = GetNextOP();
            while (Oper != null)
            {
                switch (Oper.nOPType)
                {
                    case ConnectionOP.E_OP_TYPE.SendConnect:
                        {
                            SendConnect();
                        }
                        break;

                    case ConnectionOP.E_OP_TYPE.SendDisConnect:
                        {
                            this.SendDisConnect();
                        }
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

        private bool NetCheckPackageExecute()
        {
            NetUdpReceiveFixedSizePackage mPackage = null;
            lock (mReceiveWaitCheckPackageQueue)
            {
                if (mReceiveWaitCheckPackageQueue.TryDequeue(out mPackage))
                {
                    if (!mPackage.orInnerCommandPackage())
                    {
                        nCurrentCheckPackageCount--;
                    }
                }
            }

            if (mPackage != null)
            {
                UdpStatistical.AddReceivePackageCount();
                mUdpCheckMgr.ReceiveNetPackage(mPackage);
                return true;
            }

            return false;
        }

    }
}

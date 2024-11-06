/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal class UdpCheckMgr : UdpCheckMgrInterface
    {
        readonly UdpCheckMgrInterface mMgr = null;

        public UdpCheckMgr(UdpClientPeerCommonBase mClientPeer)
        {
            this.mMgr = new UdpCheckMgr1(mClientPeer);
        }

        public void SetSureOrderId(NetUdpFixedSizePackage mPackage)
        {
            this.mMgr.SetSureOrderId(mPackage);
        }

        public void ReceiveNetPackage(NetUdpFixedSizePackage mReceivePackage)
        {
            mMgr.ReceiveNetPackage(mReceivePackage);
        }

        public void SendLogicPackage(ushort id, ReadOnlySpan<byte> buffer)
        {
            mMgr.SendLogicPackage(id, buffer);
        }

        public void Update(double elapsed)
        {
           mMgr.Update(elapsed);
        }

        public void Reset()
        {
            mMgr.Reset();
        }

        public void Release()
        {
            mMgr.Release();
        }
    }

    internal interface UdpCheckMgrInterface
    {
        void SetSureOrderId(NetUdpFixedSizePackage mPackage);
        void SendLogicPackage(UInt16 id, ReadOnlySpan<byte> buffer);
        void ReceiveNetPackage(NetUdpFixedSizePackage mReceivePackage);
        void Update(double elapsed);
        void Reset();
        void Release();
    }
}
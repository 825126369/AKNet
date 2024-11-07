using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;
using System;

namespace AKNet.Udp.POINTTOPOINT.Client
{
    internal class UdpSocket2Mgr
    {
        private double fConnectCdTime = 0.0;
        public const double fConnectMaxCdTime = 2.0;
        private ClientPeer mClientPeer;
        private UdpSocket2 mUdpSocket2;

        public UdpSocket2Mgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            mUdpSocket2 = new UdpSocket2(mClientPeer);
        }

        public void Update(double elapsed)
        {
            fConnectCdTime += elapsed;
            if (fConnectCdTime >= fConnectMaxCdTime)
            {
                mClientPeer.mSocketMgr.ConnectServer();
            }
        }

        //public void SendConnectPackage(id)
        //{
        //    NetLog.Assert(UdpNetCommand.orInnerCommand(id));
        //    NetUdpFixedSizePackage mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
        //    mPackage.nPackageId = id;
        //    mPackage.Length = Config.nUdpPackageFixedHeadSize;
        //    mClientPeer.SendNetPackage(mPackage);
        //}

        public void SendConnect()
        {
            NetLog.Log("UdpSocket2Mgr: Udp 正在连接服务器: " + mClientPeer.mSocketMgr.GetIPEndPoint());
            mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_CONNECT);
        }

        public void ReceiveConnect(NetUdpFixedSizePackage mPackage)
        {
            NetLog.Log("Client: Udp连接服务器 成功 ! ");
        }

        private void Reset()
        {
            fConnectCdTime = 0.0;
        }

    }
}

/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:26:51
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp2Tcp.Common;

namespace AKNet.Udp2Tcp.Server
{
    internal class MsgReceiveMgr
	{
        private UdpServer mNetServer = null;
        private ClientPeerPrivate mClientPeer = null;
        private readonly NetStreamCircularBuffer mReceiveStreamList = null;

        public MsgReceiveMgr(UdpServer mNetServer, ClientPeerPrivate mClientPeer)
        {
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
            mReceiveStreamList = new NetStreamCircularBuffer();
        }

        public int GetCurrentFrameRemainPackageCount()
        {
            return mClientPeer.mSocketMgr.GetCurrentFrameRemainPackageCount();
        }

        public void Update(double elapsed)
        {
            while (GetReceiveCheckPackage())
            {

            }

            while (NetTcpPackageExecute())
            {

            }
        }

        private bool GetReceiveCheckPackage()
        {
            NetUdpFixedSizePackage mPackage = null;
            if (mClientPeer.mSocketMgr.GetReceivePackage(out mPackage))
            {
                UdpStatistical.AddReceivePackageCount();
                NetLog.Assert(mPackage != null, "mPackage == null");
                mClientPeer.mUdpCheckPool.ReceiveNetPackage(mPackage);
                return true;
            }
            return false;
        }

        public void ReceiveTcpStream(NetUdpFixedSizePackage mPackage)
        {
            mReceiveStreamList.WriteFrom(mPackage.GetTcpBufferSpan());
        }

        private bool NetTcpPackageExecute()
        {
            var mNetPackage = mNetServer.GetLikeTcpNetPackage();
            bool bSuccess = mNetServer.mCryptoMgr.Decode(mReceiveStreamList, mNetPackage);
            if (bSuccess)
            {
                mClientPeer.NetPackageExecute(mNetPackage);
            }
            return bSuccess;
        }

        public void Reset()
		{
            
        }

        public void Release()
        {
            mReceiveStreamList.Dispose();
        }

    }
}
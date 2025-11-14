/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:27
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp3Tcp.Common;

namespace AKNet.Udp3Tcp.Server
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
        }

        private bool GetReceiveCheckPackage()
        {
            NetUdpReceiveFixedSizePackage mPackage = null;
            if (mClientPeer.mSocketMgr.GetReceivePackage(out mPackage))
            {
                UdpStatistical.AddReceivePackageCount();
                NetLog.Assert(mPackage != null, "mPackage == null");
                mClientPeer.mUdpCheckPool.ReceiveNetPackage(mPackage);
                return true;
            }
            return false;
        }

        public void ReceiveTcpStream(NetUdpReceiveFixedSizePackage mPackage)
        {
            mReceiveStreamList.WriteFrom(mPackage.GetTcpBufferSpan());
            while (NetTcpPackageExecute())
            {

            }
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
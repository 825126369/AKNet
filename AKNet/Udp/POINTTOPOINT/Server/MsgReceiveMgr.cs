/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:43
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class MsgReceiveMgr
	{
        private UdpServer mNetServer = null;
        private ClientPeerPrivate mClientPeer = null;
		
		public MsgReceiveMgr(UdpServer mNetServer, ClientPeerPrivate mClientPeer)
        {
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
		}

        public int GetCurrentFrameRemainPackageCount()
        {
            return mClientPeer.mSocketMgr.GetCurrentFrameRemainPackageCount();
        }

        public void Update(double elapsed)
        {
            while (GetReceivePackage())
            {

            }
        }

        private bool GetReceivePackage()
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

        public void Reset()
		{
            
        }
	}
}
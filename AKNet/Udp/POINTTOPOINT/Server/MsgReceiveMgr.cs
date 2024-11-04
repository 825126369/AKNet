/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class MsgReceiveMgr
	{
        private UdpServer mNetServer = null;
        private ClientPeer mClientPeer = null;
		
		public MsgReceiveMgr(UdpServer mNetServer, ClientPeer mClientPeer)
        {
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
		}

		public void AddLogicHandleQueue(NetPackage mPackage)
		{
            NetPackageExecute(mClientPeer, mPackage);
        }

		private void NetPackageExecute(ClientPeer clientPeer, NetPackage mPackage)
		{
			mNetServer.GetPackageManager().NetPackageExecute(clientPeer, mPackage);
            mNetServer.GetObjectPoolManager().Recycle(mPackage);
        }

		public void Update(double elapsed)
		{
			
		}

		public void Reset()
		{
			
		}
	}
}
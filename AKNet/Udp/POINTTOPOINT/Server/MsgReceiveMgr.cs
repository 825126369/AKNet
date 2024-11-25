/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Tcp.Common;
using AKNet.Udp.POINTTOPOINT.Common;
using System.Collections.Generic;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class MsgReceiveMgr
	{
        private readonly Queue<NetUdpFixedSizePackage> mWaitCheckPackageQueue = new Queue<NetUdpFixedSizePackage>();
        private UdpServer mNetServer = null;
        private ClientPeer mClientPeer = null;
		
		public MsgReceiveMgr(UdpServer mNetServer, ClientPeer mClientPeer)
        {
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
		}

        public int GetCurrentFrameRemainPackageCount()
        {
            return mWaitCheckPackageQueue.Count;
        }

        public void ReceiveWaitCheckNetPackage(NetUdpFixedSizePackage mPackage)
		{
            lock (mWaitCheckPackageQueue)
            {
                mWaitCheckPackageQueue.Enqueue(mPackage);
            }
        }

        public void Update(double elapsed)
        {
            while (NetPackageExecute())
            {

            }
        }

        private bool NetPackageExecute()
        {
            NetUdpFixedSizePackage mPackage = null;
            lock (mWaitCheckPackageQueue)
            {
                mWaitCheckPackageQueue.TryDequeue(out mPackage);
            }

            if (mPackage != null)
            {
                UdpStatistical.AddReceivePackageCount();
                mClientPeer.mUdpCheckPool.ReceiveNetPackage(mPackage);
                return true;
            }

            return false;
        }

        public void Reset()
		{
            lock (mWaitCheckPackageQueue)
            {
                while (mWaitCheckPackageQueue.Count > 0)
                {
                    var mPackage = mWaitCheckPackageQueue.Dequeue();
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }
        }
	}
}
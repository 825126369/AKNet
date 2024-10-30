/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections.Concurrent;
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;

namespace AKNet.Udp.POINTTOPOINT.Client
{
    internal class UdpPackageMainThreadMgr
    {
        private readonly ConcurrentQueue<NetUdpFixedSizePackage> mPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
        private ClientPeer mClientPeer = null;

		public UdpPackageMainThreadMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
        }

        public void Update(double elapsed)
        {
            NetUdpFixedSizePackage mPackage = null;
            while (mPackageQueue.TryDequeue(out mPackage))
            {
                PackageStatistical.AddReceivePackageCount();
                mClientPeer.mUdpCheckPool.ReceiveNetPackage(mPackage);
            }
        }

        public void MultiThreadingReceiveNetPackage(NetUdpFixedSizePackage mPackage)
        {
            bool bSucccess = NetPackageEncryption.DeEncryption(mPackage);
            if (bSucccess)
            {
                mPackageQueue.Enqueue(mPackage);
            }
            else
            {
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                NetLog.LogError("解码失败 !!!");
            }
        }
    }
}
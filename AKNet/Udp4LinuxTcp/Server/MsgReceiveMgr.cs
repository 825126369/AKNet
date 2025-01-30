/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4LinuxTcp.Common;

namespace AKNet.Udp4LinuxTcp.Server
{
    internal class MsgReceiveMgr
	{
        private UdpServer mNetServer = null;
        private ClientPeer mClientPeer = null;
        private readonly AkCircularBuffer mReceiveStreamList = null;
        private readonly msghdr mTcpMsg = new msghdr(); 

        public MsgReceiveMgr(UdpServer mNetServer, ClientPeer mClientPeer)
        {
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
            mReceiveStreamList = new AkCircularBuffer();
        }

        public void Update(double elapsed)
        {
            while (GetReceiveCheckPackage())
            {

            }

            ReceiveTcpStream();
        }

        private bool GetReceiveCheckPackage()
        {
            sk_buff mPackage = null;
            if (mClientPeer.mSocketMgr.GetReceivePackage(out mPackage))
            {
                UdpStatistical.AddReceivePackageCount();
                NetLog.Assert(mPackage != null, "mPackage == null");
                mClientPeer.mUdpCheckPool.ReceiveNetPackage(mPackage);
                return true;
            }
            return false;
        }

        public void ReceiveTcpStream()
        {
            mClientPeer.mUdpCheckPool.ReceiveTcpStream(mTcpMsg);
            mReceiveStreamList.WriteFrom(mTcpMsg.mBuffer, 0, mTcpMsg.nLength);
            while (NetTcpPackageExecute())
            {

            }
        }

        private bool NetTcpPackageExecute()
        {
            var mNetPackage = mNetServer.GetLikeTcpNetPackage();
            bool bSuccess = LikeTcpNetPackageEncryption.Decode(mReceiveStreamList, mNetPackage);
            if (bSuccess)
            {
                mClientPeer.NetPackageExecute(mNetPackage);
            }
            return bSuccess;
        }

        public void Reset()
		{
            
        }
	}
}
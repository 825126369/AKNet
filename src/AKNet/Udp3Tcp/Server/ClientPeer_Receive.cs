/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:51
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp3Tcp.Common;

namespace AKNet.Udp3Tcp.Server
{
    internal partial class ClientPeer
    {
        private bool GetReceiveCheckPackage()
        {
            NetUdpReceiveFixedSizePackage mPackage = null;
            if (GetReceivePackage(out mPackage))
            {
                UdpStatistical.AddReceivePackageCount();
                NetLog.Assert(mPackage != null, "mPackage == null");
                mUdpCheckPool.ReceiveNetPackage(mPackage);
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
            var mNetPackage = mServerMgr.GetLikeTcpNetPackage();
            bool bSuccess = mServerMgr.GetCryptoMgr().Decode(mReceiveStreamList, mNetPackage);
            if (bSuccess)
            {
                NetPackageExecute(mNetPackage);
            }
            return bSuccess;
        }
    }
}
/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Udp4Tcp.Common;

namespace AKNet.Udp4Tcp.Server
{
    internal partial class ClientPeer
    {
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
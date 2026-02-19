/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp5Tcp.Server
{
    internal partial class ClientPeer
    {
        private void MultiThreadingReceiveStream(ReadOnlySpan<byte> e)
        {
            lock (mReceiveStreamList)
            {
                mReceiveStreamList.WriteFrom(e);
            }
        }

        private bool NetPackageExecute()
        {
            NetStreamReceivePackage mNetStreamPackage = mServerMgr.GetNetStreamPackage();
            bool bSuccess = false;
            lock (mReceiveStreamList)
            {
                bSuccess = mServerMgr.GetCryptoMgr().Decode(mReceiveStreamList, mNetStreamPackage);
            }

            if (bSuccess)
            {
                if (TcpNetCommand.orInnerCommand(mNetStreamPackage.nPackageId))
                {

                }
                else
                {
                    mServerMgr.GetPackageManager().NetPackageExecute(this, mNetStreamPackage);
                }
            }

            return bSuccess;
        }

    }
}
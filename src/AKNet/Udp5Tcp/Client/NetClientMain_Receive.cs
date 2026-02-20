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
using AKNet.Udp5Tcp.Common;
using System;

namespace AKNet.Udp5Tcp.Client
{
    internal partial class NetClientMain
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
            bool bSuccess = false;

            lock (mReceiveStreamList)
            {
                bSuccess = mCryptoMgr.Decode(mReceiveStreamList, mNetPackage);
            }

            if (bSuccess)
            {
                if (TcpNetCommand.orInnerCommand(mNetPackage.nPackageId))
                {

                }
                else
                {
                    mPackageManager.NetPackageExecute(this, mNetPackage);
                }
            }

            return bSuccess;
        }
    }
}
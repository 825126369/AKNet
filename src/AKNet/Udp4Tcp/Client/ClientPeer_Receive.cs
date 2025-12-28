/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4Tcp.Common;

namespace AKNet.Udp4Tcp.Client
{
    internal partial class ClientPeer
    {
        private void MultiThreadingReceiveSocketStream(ConnectionEventArgs e)
        {
            lock (mReceiveStreamList)
            {
                mReceiveStreamList.WriteFrom(e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred));
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
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
using System;
using System.Net;

namespace AKNet.Udp4Tcp.Server
{
    internal partial class ClientPeer
    {
        public void HandleConnectedSocket(ConnectionPeer mConnectionPeer)
        {
            MainThreadCheck.Check();
            NetLog.Assert(mConnectionPeer != null, "mConnectionPeer == null");
            this.mConnectionPeer = mConnectionPeer;
            SetSocketState(SOCKET_PEER_STATE.CONNECTED);
        }

        public IPEndPoint GetIPEndPoint()
        {
            if (mConnectionPeer != null)
            {
                return mConnectionPeer.RemoteEndPoint;
            }
            else
            {
                return null;
            }
        }

        public int GetCurrentFrameRemainPackageCount()
        {
            return mConnectionPeer.GetCurrentFrameRemainPackageCount();
        }

        public bool GetReceivePackage(out NetUdpReceiveFixedSizePackage mPackage)
        {
            return mConnectionPeer.GetReceivePackage(out mPackage);
        }

        public void SendTcpStream(ReadOnlySpan<byte> data)
        {
            MainThreadCheck.Check();
            mConnectionPeer.AddTcpStream(data);
        }

        public void CloseSocket()
        {
            if (mConnectionPeer != null)
            {
                mConnectionPeer.Close();
                mConnectionPeer = null;
            }
        }

    }
}

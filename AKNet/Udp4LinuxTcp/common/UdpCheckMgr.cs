/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.LinuxTcp;
using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class UdpCheckMgr
    {
        private UdpClientPeerCommonBase mClientPeer = null;
        private readonly tcp_sock mTcpSock = new tcp_sock();

        public UdpCheckMgr(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            LinuxTcpFunc.Init(mTcpSock);
        }

        public void SendTcpStream(ReadOnlySpan<byte> buffer)
        {
            MainThreadCheck.Check();
            if (this.mClientPeer.GetSocketState() != SOCKET_PEER_STATE.CONNECTED) return;
#if DEBUG
            if (buffer.Length > Config.nMaxDataLength)
            {
                NetLog.LogError("超出允许的最大包尺寸：" + Config.nMaxDataLength);
            }
#endif
            LinuxTcpFunc.SendTcpStream(mTcpSock, buffer);
        }

        public void ReceiveNetPackage(NetUdpReceiveFixedSizePackage mReceivePackage)
        {
            byte nInnerCommandId = mReceivePackage.GetInnerCommandId();
            MainThreadCheck.Check();
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                this.mClientPeer.ReceiveHeartBeat();
                if (nInnerCommandId == UdpNetCommand.COMMAND_HEARTBEAT)
                {

                }
                else if (nInnerCommandId == UdpNetCommand.COMMAND_CONNECT)
                {
                    this.mClientPeer.ReceiveConnect();
                }
                else if (nInnerCommandId == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    this.mClientPeer.ReceiveDisConnect();
                }

                if (UdpNetCommand.orInnerCommand(nInnerCommandId))
                {
                    mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(mReceivePackage);
                }
                else
                {
                    LinuxTcpFunc.CheckReceivePackageLoss(mTcpSock, mReceivePackage);
                }
            }
            else
            {
                if (nInnerCommandId == UdpNetCommand.COMMAND_CONNECT)
                {
                    this.mClientPeer.ReceiveConnect();
                }
                else if (nInnerCommandId == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    this.mClientPeer.ReceiveDisConnect();
                }
                mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(mReceivePackage);
            }
        }

        public void Update(double elapsed)
        {
            if (mClientPeer.GetSocketState() != SOCKET_PEER_STATE.CONNECTED) return;
            LinuxTcpFunc.Update(mTcpSock, elapsed);
        }

        public void Reset()
        {
            LinuxTcpFunc.Reset(mTcpSock);
        }
    }
}
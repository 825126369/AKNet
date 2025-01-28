/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
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
            mTcpSock.mClientPeer = mClientPeer;
            LinuxTcpFunc.Init(mTcpSock);
        }

        public void SendInnerNetData(byte nInnerCommandId)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(nInnerCommandId));

            var skb = new sk_buff();
            int tcp_options_size = 0;
            int tcp_header_size = 0;
            LinuxTcpFunc.tcp_hdr(skb).commandId = nInnerCommandId;
            if (nInnerCommandId == UdpNetCommand.COMMAND_CONNECT)
            {
                tcp_out_options opts = new tcp_out_options();
                opts.mss = (ushort)IPAddressHelper.GetMtu();
                tcp_options_size = LinuxTcpFunc.get_tcp_connect_options(mTcpSock, skb, opts);
                LinuxTcpFunc.tcp_options_write(skb, mTcpSock, opts);
            }

            tcp_header_size = LinuxTcpFunc.sizeof_tcphdr + tcp_options_size;
            skb.nBufferLength = tcp_header_size;
            skb.nBufferOffset = LinuxTcpFunc.max_tcphdr_length - tcp_header_size;

            LinuxTcpFunc.tcp_hdr(skb).window = (ushort)Math.Min(mTcpSock.rcv_wnd, 65535);
            LinuxTcpFunc.tcp_hdr(skb).tot_len = (ushort)tcp_header_size;
            LinuxTcpFunc.tcp_hdr(skb).WriteTo(skb);
            mClientPeer.SendNetPackage(skb);
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

        public void ReceiveNetPackage(sk_buff skb)
        {
            byte nInnerCommandId = LinuxTcpFunc.tcp_hdr(skb).commandId;
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
                    LinuxTcpFunc.tcp_parse_options(LinuxTcpFunc.sock_net(mTcpSock), skb, mTcpSock.rx_opt, false);
                    LinuxTcpFunc.tcp_connect_finish_init(mTcpSock, skb);
                }
                else if (nInnerCommandId == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    this.mClientPeer.ReceiveDisConnect();
                }

                if (UdpNetCommand.orInnerCommand(nInnerCommandId))
                {
                    mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(skb);
                }
                else
                {
                    LinuxTcpFunc.CheckReceivePackageLoss(mTcpSock, skb);
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
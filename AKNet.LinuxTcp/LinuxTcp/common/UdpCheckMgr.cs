/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:39
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.LinuxTcp.Common
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

        public tcp_sock GetTcpSock()
        {
            return mTcpSock;
        }

        public void InitConnect()
        {
            LinuxTcpFunc.tcp_v4_connect(mTcpSock);
        }

        public void FinishConnect(sk_buff skb)
        {
            LinuxTcpFunc.tcp_connect_finish_init(mTcpSock, skb);
        }

        public void SendInnerNetData(byte nInnerCommandId)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(nInnerCommandId));

            var skb = mClientPeer.GetObjectPoolManager().Skb_Pop();
            int tcp_options_size = 0;
            int tcp_header_size = 0;

            tcp_out_options opts = mTcpSock.snd_opts;
            var tcphdr = LinuxTcpFunc.tcp_hdr(skb);
            tcphdr.commandId = nInnerCommandId;
            if (nInnerCommandId == UdpNetCommand.COMMAND_CONNECT)
            {
                tcphdr.seq = mTcpSock.write_seq;
                tcp_options_size = LinuxTcpFunc.tcp_syn_options(mTcpSock, skb, opts);
            }

            tcp_header_size = LinuxTcpFunc.sizeof_tcphdr + tcp_options_size;
            skb.nBufferLength = tcp_header_size;
            skb.nBufferOffset = LinuxTcpFunc.max_tcphdr_length - tcp_header_size;

            tcphdr.window = (ushort)Math.Min(mTcpSock.rcv_wnd, 65535);
            tcphdr.doff = (byte)tcp_header_size;
            tcphdr.tot_len = (ushort)tcp_header_size;
            tcphdr.WriteTo(skb);
            LinuxTcpFunc.tcp_options_write(skb, mTcpSock, opts);
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

        public bool ReceiveTcpStream(msghdr msg)
        {
            MainThreadCheck.Check();
            return LinuxTcpFunc.ReceiveTcpStream(mTcpSock, msg);
        }

        public void ReceiveNetPackage(sk_buff skb)
        {
            byte nInnerCommandId = LinuxTcpFunc.tcp_hdr(skb).commandId;
            MainThreadCheck.Check();

            if (nInnerCommandId == UdpNetCommand.COMMAND_CONNECT)
            {
                this.mClientPeer.ReceiveConnect(skb);
            }

            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                this.mClientPeer.ReceiveHeartBeat();
                if (nInnerCommandId == UdpNetCommand.COMMAND_HEARTBEAT)
                {

                }
                else if (nInnerCommandId == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    this.mClientPeer.ReceiveDisConnect();
                }

                if (!UdpNetCommand.orInnerCommand(nInnerCommandId))
                {
                    LinuxTcpFunc.CheckReceivePackageLoss(mTcpSock, skb);
                }
            }

            if (UdpNetCommand.orInnerCommand(nInnerCommandId))
            {
                mClientPeer.GetObjectPoolManager().Skb_Recycle(skb);
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
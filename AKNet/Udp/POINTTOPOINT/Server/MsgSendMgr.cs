/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class MsgSendMgr
	{
        private UdpServer mNetServer = null;
        private ClientPeer mClientPeer = null;

		public MsgSendMgr(UdpServer mNetServer, ClientPeer mClientPeer)
		{
			this.mNetServer = mNetServer;
			this.mClientPeer = mClientPeer;
		}

        public void SendInnerNetData(UInt16 id)
        {
            NetLog.Assert(UdpNetCommand.orInnerCommand(id));
            NetUdpFixedSizePackage mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
            mPackage.nPackageId = id;
            mPackage.Length = Config.nUdpPackageFixedHeadSize;
            mClientPeer.SendNetPackage(mPackage);
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                SendNetData(mNetPackage.nPackageId, mNetPackage.GetBuffBody());
            }
        }

        public void SendNetData(UInt16 id)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                NetLog.Assert(UdpNetCommand.orNeedCheck(id));
                mClientPeer.mUdpCheckPool.SendLogicPackage(id, ReadOnlySpan<byte>.Empty);
            }
        }

        public void SendNetData(UInt16 id, IMessage data)
		{
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                NetLog.Assert(UdpNetCommand.orNeedCheck(id));
                if (data != null)
                {
                    byte[] cacheSendBuffer = mNetServer.GetObjectPoolManager().EnSureSendBufferOk(data);
                    ReadOnlySpan<byte> stream = Protocol3Utility.SerializePackage(data, cacheSendBuffer);
                    mClientPeer.mUdpCheckPool.SendLogicPackage(id, stream);
                }
                else
                {
                    mClientPeer.mUdpCheckPool.SendLogicPackage(id, ReadOnlySpan<byte>.Empty);
                }
            }
		}

        public void SendNetData(UInt16 id, byte[] data)
        {
            SendNetData(id, data.AsSpan());
        }

        public void SendNetData(UInt16 id, ReadOnlySpan<byte> data)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                NetLog.Assert(UdpNetCommand.orNeedCheck(id));
                mClientPeer.mUdpCheckPool.SendLogicPackage(id, data);
            }
        }
    }

}
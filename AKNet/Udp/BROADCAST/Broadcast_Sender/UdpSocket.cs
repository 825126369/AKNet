/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:28
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net;
using System.Net.Sockets;
using AKNet.Common;

namespace AKNet.Udp.BROADCAST.Sender
{
    public class SocketUdp_Basic
	{
		private EndPoint remoteSendBroadCastEndPoint = null;
		private Socket mSocket = null;

		public void InitNet(UInt16 ServerPort)
		{
			mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			IPEndPoint iep = new IPEndPoint(IPAddress.Broadcast, ServerPort);
			remoteSendBroadCastEndPoint = (EndPoint)iep;
			mSocket.EnableBroadcast = true;

			NetLog.Log("初始化 广播发送器 成功");
		}

		public void SendNetStream(byte[] msg, int offset, int count)
		{
			try
			{
				mSocket.SendTo(msg, offset, count, SocketFlags.None, remoteSendBroadCastEndPoint);
			}
			catch { }
		}

		private void CloseNet()
		{
			mSocket.Close();
		}

		public virtual void Release()
		{
			this.CloseNet();
			NetLog.Log("--------------- BroadcastSender  Release ----------------");
		}
	}
}










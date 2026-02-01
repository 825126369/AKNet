/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:44
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet.LinuxTcp")]
[assembly: InternalsVisibleTo("AKNet.WebSocket")]
namespace AKNet.Common
{
    internal class ListenClientPeerStateMgr
	{
		private event Action<ClientPeerBase, SOCKET_PEER_STATE> mEventFunc1 = null;
		private event Action<ClientPeerBase> mEventFunc2 = null;

		public void OnSocketStateChanged(ClientPeerBase mClientPeer)
		{
			MainThreadCheck.Check();
			mEventFunc2?.Invoke(mClientPeer);
			mEventFunc1?.Invoke(mClientPeer, mClientPeer.GetSocketState());
		}

        public void addListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> func)
		{
			mEventFunc1 += func;
		}

		public void removeListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> func)
		{
			mEventFunc1 -= func;
		}

		public void addListenClientPeerStateFunc(Action<ClientPeerBase> func)
		{
			mEventFunc2 += func;
		}

		public void removeListenClientPeerStateFunc(Action<ClientPeerBase> func)
		{
			mEventFunc2 -= func;
		}
	}
}

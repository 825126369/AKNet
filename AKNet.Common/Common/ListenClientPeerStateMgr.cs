/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet2")]
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

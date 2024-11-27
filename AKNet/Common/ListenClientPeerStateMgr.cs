/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:04
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Common
{
    internal class ListenClientPeerStateMgr
	{
		private Action<ClientPeerBase, SOCKET_PEER_STATE> mEventFunc1 = null;
		private Action<ClientPeerBase> mEventFunc2 = null;
		private SOCKET_PEER_STATE mSocketState = SOCKET_PEER_STATE.NONE;
		private bool b_SOCKET_PEER_STATE_Changed = false;

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

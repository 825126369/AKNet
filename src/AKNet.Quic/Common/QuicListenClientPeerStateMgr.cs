/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:01
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    internal class QuicListenClientPeerStateMgr
	{
		private event Action<QuicClientPeerBase, SOCKET_PEER_STATE> mEventFunc1 = null;
		private event Action<QuicClientPeerBase> mEventFunc2 = null;

		public void OnSocketStateChanged(QuicClientPeerBase mClientPeer)
		{
			MainThreadCheck.Check();
			mEventFunc2?.Invoke(mClientPeer);
			mEventFunc1?.Invoke(mClientPeer, mClientPeer.GetSocketState());
		}

        public void addListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> func)
		{
			mEventFunc1 += func;
		}

		public void removeListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> func)
		{
			mEventFunc1 -= func;
		}

		public void addListenClientPeerStateFunc(Action<QuicClientPeerBase> func)
		{
			mEventFunc2 += func;
		}

		public void removeListenClientPeerStateFunc(Action<QuicClientPeerBase> func)
		{
			mEventFunc2 -= func;
		}
	}
}

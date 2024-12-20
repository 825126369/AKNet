/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    /* 客户端 状态 
		NONE = 0,

		CONNECTING = 1,
		CONNECTED = 2,

		DISCONNECTING = 3,
		DISCONNECTED = 4,

		RECONNECTING = 5,
	*/

    /* 服务器 状态 
		NONE = 0,
		CONNECTED = 2,
		DISCONNECTED = 4,
	*/

    public enum SOCKET_PEER_STATE : uint
	{
		NONE = 0,

		CONNECTING = 1,
		CONNECTED = 2,

		DISCONNECTING = 3,
		DISCONNECTED = 4,

		RECONNECTING = 5,
	}

	public enum SOCKET_SERVER_STATE : uint
	{
		NONE = 0,
		NORMAL = 1,
		EXCEPTION = 2,
	}
}
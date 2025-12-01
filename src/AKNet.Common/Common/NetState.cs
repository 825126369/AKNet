/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:14
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
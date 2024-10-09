namespace XKNet.Common
{
	public enum CLIENT_SOCKET_PEER_STATE : uint
	{
		NONE = 0,

		CONNECTING = 1,
		CONNECTED = 2,

		DISCONNECTING = 3,
		DISCONNECTED = 4,

		RECONNECTING = 5,
	}

    public enum SERVER_SOCKET_PEER_STATE : uint
    {
        NONE = 0,
        CONNECTED = 1,
        DISCONNECTED = 2,
    }
}
enum StreamType
{
	Chat,
	ZhenTongBu,
	Other
}﻿

1: QUIC 虽然有多个流，但是如何让多个流和 自定会议逻辑层 StreamType 枚举对应，QUIC 并没有实现。
所以这个AKNet.QUIC库，就要实现这个流和逻辑层 一一对应的关系。
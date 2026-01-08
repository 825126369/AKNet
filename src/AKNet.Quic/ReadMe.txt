enum StreamType
{
	Chat,
	ZhenTongBu,
	Other
}﻿

1: QUIC 虽然有多个流，但是如何让多个流和 自定义逻辑层 StreamType 枚举对应，QUIC 并没有实现。
所以这个AKNet.QUIC库，就要实现这个流和逻辑层 一一对应的关系。

2:经详细分析: 不需要一一对应。 服务器有自定义的流类型，客户端也有自定义的流类型，2者相互独立

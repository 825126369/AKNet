/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:53
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace MSQuic2
{
    internal static partial class MSQuicFunc
    {
        public const int sizeof_QUIC_RETRY_PACKET_V1 = 10;
        public const int sizeof_QUIC_VERSION_NEGOTIATION_PACKET = 8;
        public const int sizeof_QUIC_TOKEN_CONTENTS = 100;
        public const int sizeof_QuicVersion = sizeof(uint);
    }
}

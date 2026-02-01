/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:55
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Extentions.Protobuf;

namespace TestProtocol
{
    public sealed partial class TESTChatMessage : IProtobufResetInterface
    {
        public void Reset()
        {
            NClientId = 0;
            NSortId = 0;
            TalkMsg = string.Empty;
            PeerName = string.Empty;
        }
    }
}


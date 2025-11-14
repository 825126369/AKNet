/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:28
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal interface UdpClientPeerBase
    {
        void SetName(string Name);
        void Reset();
    }
}

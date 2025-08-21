/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Udp1MSQuic.Common;
using System.Net;

namespace AKNet.QuicNet.Server
{
    internal interface TcpClientPeerBase
    {
        void SetName(string Name);
        void HandleConnectedSocket(QuicConnection mQuicConnection);
        void Update(double elapsed);
        void Reset();
        IPEndPoint GetIPEndPoint();
    }
}

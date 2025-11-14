/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:39
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Udp2MSQuic.Common;
using System.Net;

namespace AKNet.Udp2MSQuic.Server
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

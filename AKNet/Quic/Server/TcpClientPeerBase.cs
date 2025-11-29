/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:45
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
#if NET9_0_OR_GREATER

using System.Net;
using System.Net.Quic;
using System.Net.Sockets;

namespace AKNet.Quic.Server
{
    public interface TcpClientPeerBase
    {
        void SetName(string Name);
        void HandleConnectedSocket(QuicConnection mQuicConnection);
        void Update(double elapsed);
        void Reset();
        IPEndPoint GetIPEndPoint();
    }
}

#endif

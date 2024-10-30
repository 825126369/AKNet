/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:40
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Net;
using System.Net.Sockets;

namespace AKNet.Tcp.Server
{
    public interface TcpClientPeerBase
    {
        void SetName(string Name);
        void HandleConnectedSocket(Socket mSocket);
        void Update(double elapsed);
        void Reset();
        IPEndPoint GetIPEndPoint();
    }
}

/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:38
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Net;
using System.Net.Sockets;

namespace AKNet.Tcp.Server
{
    public interface ServerClientPeerInterface
    {
        void SetName(string Name);
        void HandleConnectedSocket(Socket mSocket);
        void Update(double elapsed);
        void Reset();
        IPEndPoint GetIPEndPoint();
    }
}

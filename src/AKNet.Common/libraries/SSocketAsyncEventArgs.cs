/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:45
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net.Sockets;

namespace AKNet.Common
{
    internal class SSocketAsyncEventArgs: SocketAsyncEventArgs
    {
        public event EventHandler<SSocketAsyncEventArgs> Completed2;

        public void Do()
        {
            this.Completed2?.Invoke(null, this);
        }
    }
}

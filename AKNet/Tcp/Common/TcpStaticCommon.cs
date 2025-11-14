/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:26:51
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net.Sockets;

namespace AKNet.Tcp.Common
{
    internal static class TcpStaticCommon
    {
        public static void SetKeepAlive(Socket socket, bool bUse, UInt32 keepAliveInterval, UInt32 retryInterval)
        {
            int size = sizeof(UInt32);
            uint on = bUse ? (uint)1 : 0;

            byte[] inArray = new byte[size * 3];
            Array.Copy(BitConverter.GetBytes(on), 0, inArray, 0, size);
            Array.Copy(BitConverter.GetBytes(keepAliveInterval), 0, inArray, size, size);
            Array.Copy(BitConverter.GetBytes(retryInterval), 0, inArray, size * 2, size);
            socket.IOControl(IOControlCode.KeepAliveValues, inArray, null);
        }
    }
}

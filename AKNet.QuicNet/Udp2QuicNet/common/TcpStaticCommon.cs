/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net.Sockets;

namespace AKNet.QuicNet.Common
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

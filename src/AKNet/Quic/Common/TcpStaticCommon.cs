/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
#if NET9_0_OR_GREATER
using System;
using System.Net.Sockets;

namespace AKNet.Quic.Common
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
#endif

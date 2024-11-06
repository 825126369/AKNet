/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal static class UdpStaticCommon
    {
        static readonly Stopwatch mStopwatch = Stopwatch.StartNew();
        public static long GetNowTime()
        {
            return mStopwatch.ElapsedMilliseconds;
        }

        private static void CheckReceiveBufferUsage(Socket socket)
        {
            int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];
            int result = socket.IOControl(IOControlCode.ReceiveAll, null, buffer);

            if (result != 0)
            {
                Console.WriteLine($"Error checking receive buffer usage: {result}");
                return;
            }

            // 解析返回的缓冲区使用情况
            int usedBufferSize = BitConverter.ToInt32(buffer, 0);
            NetLog.Log($"Used receive buffer size: {usedBufferSize} bytes");
        }
    }
}

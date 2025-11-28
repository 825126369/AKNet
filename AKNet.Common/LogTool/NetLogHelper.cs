/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet2")]
namespace AKNet.Common
{
    internal static class NetLogHelper
    {
        public static void PrintByteArray(string tag, ReadOnlySpan<byte> message)
        {
            string data = tag + ": " + string.Join(' ', message.ToArray());
            NetLog.Log(data);
        }
    }
}

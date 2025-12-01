/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet2")]
[assembly: InternalsVisibleTo("AKNet.Other")]
namespace AKNet.Common
{
    internal static class TimeTool
    {
        /// <summary>
        /// 时间戳转为C#格式时间
        /// </summary>
        /// <param name=”timeStamp”></param>
        /// <returns></returns>
        public static DateTime GetDateTime(ulong timeStamp)
        {
            DateTime dtStart = TimeZoneInfo.ConvertTimeToUtc(new DateTime(1970, 1, 1));
            return dtStart.AddSeconds(timeStamp);
        }

        /// <summary>
        /// DateTime时间格式转换为Unix时间戳格式
        /// </summary>
        /// <param name=”time”></param>
        /// <returns></returns>
        public static long GetTimeStamp(System.DateTime time)
        {
            DateTime startTime = TimeZoneInfo.ConvertTimeToUtc(new DateTime(1970, 1, 1));
            return (long)(time - startTime).TotalSeconds;
        }

        public static long GetTimeStamp()
        {
            System.DateTime time = DateTime.Now;
            DateTime startTime = TimeZoneInfo.ConvertTimeToUtc(new DateTime(1970, 1, 1));
            return (long)(time - startTime).TotalSeconds;
        }
    }
}

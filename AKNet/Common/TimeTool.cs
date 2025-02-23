using System;

namespace AKNet.Common
{
    public static class TimeTool
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

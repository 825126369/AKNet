/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        public static bool WARN_ON(bool condition)
        {
            if (condition)
            {
                NetLog.LogWarning(condition);
            }

            return condition;
        }

        public static bool BUG_ON(bool condition)
        {
            if (condition)
            {
                NetLog.LogError(condition);
            }

            return condition;
        }
    }
}

/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:46
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Common
{
    public static partial class VersionPublishConfig
    {
        public static readonly DateTime m_BuildTime;
        public static readonly System.Version m_Version;
        // 在不同的应用上，虽然DateTime 一样，但转为ToString() 后，字符串不一样
    }
}
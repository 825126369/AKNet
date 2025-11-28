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

namespace AKNet.Common.Channel
{
    public partial class ChannelClosedException : InvalidOperationException
    {
        public ChannelClosedException() :
            base() { }

        public ChannelClosedException(string? message) : base(message) { }
        public ChannelClosedException(Exception? innerException) : base() { }
        public ChannelClosedException(string? message, Exception? innerException) : base(message) { }
    }
}

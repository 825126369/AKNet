/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:05
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Winsock
        {
            public const uint ERROR_SUCCESS = 0;
            public const uint ERROR_HANDLE_EOF = 38;
            public const uint ERROR_NOT_SUPPORTED = 50;
            public const uint ERROR_INVALID_PARAMETER = 87;
            public const uint ERROR_ALREADY_EXISTS = 183;
            public const uint ERROR_MORE_DATA = 234;
            public const uint ERROR_OPERATION_ABORTED = 995;
            public const uint ERROR_IO_PENDING = 997;
            public const uint ERROR_NOT_FOUND = 1168;
            public const uint ERROR_CONNECTION_INVALID = 1229;
        }
    }
}

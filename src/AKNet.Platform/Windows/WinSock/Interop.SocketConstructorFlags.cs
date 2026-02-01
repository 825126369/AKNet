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
            [Flags]
            internal enum SocketConstructorFlags
            {
                WSA_FLAG_OVERLAPPED = 0x01,
                WSA_FLAG_MULTIPOINT_C_ROOT = 0x02,
                WSA_FLAG_MULTIPOINT_C_LEAF = 0x04,
                WSA_FLAG_MULTIPOINT_D_ROOT = 0x08,
                WSA_FLAG_MULTIPOINT_D_LEAF = 0x10,
                WSA_FLAG_NO_HANDLE_INHERIT = 0x80,
            }
        }
    }
}

/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:04
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
#if NET7_0_OR_GREATER
        public static unsafe partial class BCrypt
        {
            internal const int BCRYPT_USE_SYSTEM_PREFERRED_RNG = 0x00000002;

            [LibraryImport(Libraries.BCrypt)]
            public static unsafe partial int BCryptGenRandom(IntPtr hAlgorithm, byte* pbBuffer, int cbBuffer, int dwFlags);
        }
#else
        public static unsafe partial class BCrypt
        {
            [DllImport(Libraries.BCrypt)]
            public static unsafe extern int BCryptGenRandom(IntPtr hAlgorithm, byte* pbBuffer, int cbBuffer, int dwFlags);
        }
#endif
    }
}

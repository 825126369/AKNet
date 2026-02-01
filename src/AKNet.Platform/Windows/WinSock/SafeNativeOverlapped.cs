/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    internal sealed class SafeNativeOverlapped : SafeHandle
    {
        private readonly IntPtr _socketHandle;

        public SafeNativeOverlapped()
            : this(IntPtr.Zero)
        {
            
        }

        private SafeNativeOverlapped(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public unsafe SafeNativeOverlapped(IntPtr socketHandle, NativeOverlapped* handle)
            : this((IntPtr)handle)
        {
            _socketHandle = socketHandle;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            FreeNativeOverlapped();
            return true;
        }

        private unsafe void FreeNativeOverlapped()
        {
           
        }
    }
}

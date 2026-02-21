using System.Runtime.InteropServices;

namespace AKNet.Common
{
    public static class OSPlatformTool
    {
        /// <summary>
        /// 检测是否为桌面 PC 环境（.NET Standard 2.0 兼容）
        /// </summary>
        public static bool IsDesktopPC()
        {
            // 步骤1: 检测操作系统平台
            bool isDesktopOS = IsWindows() || IsMacOS() || IsLinux();
            if (!isDesktopOS) return false;
            return true;
        }

        /// <summary>
        /// 是否为 Windows 桌面系统
        /// </summary>
        public static bool IsWindows() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// 是否为 macOS
        /// </summary>
        public static bool IsMacOS() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// 是否为 Linux
        /// </summary>
        public static bool IsLinux() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        /// 获取操作系统描述
        /// </summary>
        public static string GetOSDescription() =>
            RuntimeInformation.OSDescription;

        /// <summary>
        /// 获取系统架构
        /// </summary>
        public static string GetOSArchitecture() =>
            RuntimeInformation.OSArchitecture.ToString();
    }
}

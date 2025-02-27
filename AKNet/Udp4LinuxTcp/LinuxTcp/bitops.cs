/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        static uint ror32(uint word, uint shift)
        {
            return (word >> (int)(shift & 31)) | (word << (int)((-shift) & 31));

            // 将 shift 限制在 0 到 31 之间
            shift &= 31;
            // 右移 shift 位
            uint rightShifted = word >> (int)shift;
            // 左移 32 - shift 位
            uint leftShifted = word << (int)(32 - shift);
            // 组合右移和左移的结果
            return rightShifted | leftShifted;
        }
    }
}

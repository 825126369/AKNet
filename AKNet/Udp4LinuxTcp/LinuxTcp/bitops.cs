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

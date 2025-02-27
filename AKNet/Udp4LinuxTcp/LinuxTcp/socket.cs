/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class msghdr
    {
        public readonly AkCircularBuffer mBuffer;
        public int nLength;
        public readonly int nMaxLength = 1500;

        public msghdr(AkCircularBuffer buffer, int nMaxLength)
        {
            this.mBuffer = buffer;
            this.nMaxLength = nMaxLength;
        }
    }
}

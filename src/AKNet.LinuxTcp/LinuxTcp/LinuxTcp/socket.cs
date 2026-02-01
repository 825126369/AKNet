/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:08
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.LinuxTcp.Common
{
    internal class msghdr
    {
        public readonly AkCircularManyBuffer mBuffer;
        public int nLength;
        public readonly int nMaxLength = 1500;

        public msghdr(AkCircularManyBuffer buffer, int nMaxLength)
        {
            this.mBuffer = buffer;
            this.nMaxLength = nMaxLength;
        }
    }
}

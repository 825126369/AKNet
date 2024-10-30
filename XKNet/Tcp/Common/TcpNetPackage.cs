/************************************Copyright*****************************************
*        ProjectName:XKNet
*        Web:https://github.com/825126369/XKNet
*        Description:XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 12:14:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using XKNet.Common;

namespace XKNet.Tcp.Common
{
    public class TcpNetPackage : NetPackage
    {
        public byte[] mBuffer;
        public int mLength;

        public override ReadOnlySpan<byte> GetBuff()
        {
            return new ReadOnlySpan<byte>(mBuffer, 0, mLength);
        }

        public override ReadOnlySpan<byte> GetBuffBody()
        {
            return new ReadOnlySpan<byte>(mBuffer, Config.nPackageFixedHeadSize, mLength - Config.nPackageFixedHeadSize);
        }

        public override ReadOnlySpan<byte> GetBuffHead()
        {
            return new ReadOnlySpan<byte>(mBuffer, 0, Config.nPackageFixedHeadSize);
        }
    }
}


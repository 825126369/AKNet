/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:35
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;

namespace AKNet.Tcp.Common
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


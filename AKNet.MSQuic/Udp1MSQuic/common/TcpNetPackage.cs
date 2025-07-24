/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;

namespace AKNet.Udp1MSQuic.Common
{
    internal class TcpNetPackage : NetPackage
    {
        public ushort nPackageId = 0;
        private ReadOnlyMemory<byte> mReadOnlyMemory;

        public void InitData(byte[] mBuffer, int nOffset, int nLength)
        {
            mReadOnlyMemory = new ReadOnlyMemory<byte>(mBuffer, nOffset, nLength);
        }
        
        public ReadOnlySpan<byte> GetData()
        {
            return mReadOnlyMemory.Span;
        }

        public ushort GetPackageId()
        {
            return nPackageId;
        }
    }
}


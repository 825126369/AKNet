/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:36
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;

namespace AKNet.Tcp.Common
{
    public class TcpNetPackage : NetPackage
    {
        private ReadOnlyMemory<byte> mReadOnlyMemory;

        public void InitData(byte[] mBuffer, int nOffset, int nLength)
        {
            mReadOnlyMemory = new ReadOnlyMemory<byte>(mBuffer, nOffset, nLength);
        }
        
        public override ReadOnlySpan<byte> GetData()
        {
            return mReadOnlyMemory.Span;
        }
    }
}


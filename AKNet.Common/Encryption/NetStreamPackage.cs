/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:43
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Common
{
    internal class NetStreamPackage : NetPackage
    {
        public ushort nPackageId = 0;
        private ReadOnlyMemory<byte> mReadOnlyMemory;

        public void SetData(Memory<byte> mData)
        {
            mReadOnlyMemory = mData;
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


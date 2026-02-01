/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:01
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    internal class QuicStreamReceivePackage : QuicNetPackage
    {
        public byte nStreamIndex = 0;
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

        public byte GetStreamEnumIndex()
        {
            return nStreamIndex;
        }
    }
}


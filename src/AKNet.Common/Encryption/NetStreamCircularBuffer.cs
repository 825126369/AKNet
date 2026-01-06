/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:14
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("AKNet")]
[assembly: InternalsVisibleTo("AKNet.MSQuic")]
[assembly: InternalsVisibleTo("AKNet.LinuxTcp")]
[assembly: InternalsVisibleTo("AKNet.WebSocket")]
[assembly: InternalsVisibleTo("AKNet.Quic")]
namespace AKNet.Common
{
    internal class NetStreamCircularBuffer:AkCircularManyBuffer
    {
        //AkCircularBuffer mBufferInterface;

        //public int Length
        //{
        //    get
        //    {
        //        return mBufferInterface.Length;
        //    }
        //}

        //public void WriteFrom(SocketAsyncEventArgs e)
        //{
        //    mBufferInterface.WriteFrom(e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred));
        //}

        //public bool isCanWriteTo(int countT)
        //{
        //    return mBufferInterface.isCanWriteTo(countT);
        //}

        //public int CopyTo(Span<byte> mTempSpan)
        //{
        //    return mBufferInterface.CopyTo(0, mTempSpan);
        //}

        //public void Reset()
        //{

        //}

        //public void Dispose()
        //{

        //}
    }
}

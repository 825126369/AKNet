/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.Udp1MSQuic.Common
{
    internal class MsQuicBuffers
    {
        private readonly QUIC_BUFFER[] _buffers = new QUIC_BUFFER[1];
        public QUIC_BUFFER[] Buffers => _buffers;
        public int Count => _buffers.Length;

        private void SetBuffer(int index, ReadOnlyMemory<byte> buffer)
        {
            NetLog.Assert(index < Count);
            if (_buffers[index] == null)
            {
                _buffers[index] = new QUIC_BUFFER(Config.nIOContexBufferLength);
            }

            _buffers[index].Offset = 0;
            _buffers[index].Length = buffer.Length;
            buffer.Span.CopyTo(_buffers[index].GetSpan());
        }

        public void Initialize(ReadOnlyMemory<byte> buffer)
        {
            SetBuffer(0, buffer);
        }

        public void Reset()
        {
            foreach (var buffer in Buffers)
            {
                buffer.Offset = 0;
                buffer.Length = 0;
            }
        }
    }
}

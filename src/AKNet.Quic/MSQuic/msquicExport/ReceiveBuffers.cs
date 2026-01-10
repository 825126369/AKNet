/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:20
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.MSQuic.Common
{
    internal class ReceiveBuffers
    {
        private const int MaxBufferedBytes = 64 * 1024;
        private readonly object _syncRoot = new object();
        private readonly AkCircularManyBuffer _buffer = new AkCircularManyBuffer();
        private bool _final;
        public bool HasCapacity()
        {
            lock (_syncRoot)
            {
                return _buffer.Length < MaxBufferedBytes;
            }
        }

        public void SetFinal()
        {
            lock (_syncRoot)
            {
                _final = true;
            }
        }

        public int WriteFrom(ReadOnlySpan<QUIC_BUFFER> quicBuffers, int totalLength, bool final)
        {
            lock (_syncRoot)
            {
                if (_buffer.Length + totalLength > MaxBufferedBytes)
                {
                    totalLength = MaxBufferedBytes - _buffer.Length;
                    final = false;
                }

                _final = final;

                int totalCopied = 0;
                foreach (var v in quicBuffers)
                {
                    Span<byte> quicBuffer = v.GetSpan();
                    if (totalLength < quicBuffer.Length)
                    {
                        quicBuffer = quicBuffer.Slice(0, totalLength);
                    }
                    _buffer.WriteFrom(quicBuffer);
                    totalCopied += quicBuffer.Length;
                    totalLength -= quicBuffer.Length;
                }
                return totalCopied;
            }
        }

        public int WriteTo(Memory<byte> buffer, out bool completed, out bool empty)
        {
            int nWriteLength = 0;
            lock (_syncRoot)
            {
                nWriteLength = _buffer.WriteTo(buffer.Span);
                completed = _buffer.IsEmpty && _final;
                empty = _buffer.IsEmpty;
            }
            return nWriteLength;
        }

    }
}

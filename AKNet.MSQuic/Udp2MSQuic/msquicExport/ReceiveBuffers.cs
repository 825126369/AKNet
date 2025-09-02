using AKNet.Common;
using System;

#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.Udp2MSQuic.Common
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
                if (_buffer.Length > MaxBufferedBytes - totalLength)
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

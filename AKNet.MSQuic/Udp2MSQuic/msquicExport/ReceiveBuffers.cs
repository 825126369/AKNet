

using AKNet.Common;
using System;

namespace AKNet.Udp2MSQuic.Common
{
    internal class ReceiveBuffers
    {
        private const int MaxBufferedBytes = 64 * 1024;
        private readonly object _syncRoot = new object();
        private readonly AkCircularManyBuffer _buffer = new AkCircularManyBuffer();

        public bool HasCapacity()
        {
            lock (_syncRoot)
            {
                return _buffer.Length < MaxBufferedBytes;
            }
        }

        public int WriteFrom(QUIC_BUFFER[] quicBuffers, int BufferCount, int totalLength)
        {
            lock (_syncRoot)
            {
                int totalCopied = 0;
                if (_buffer.Length < MaxBufferedBytes)
                {
                    foreach (var v in quicBuffers)
                    {
                        if (_buffer.Length + v.Length <= MaxBufferedBytes)
                        {
                            _buffer.WriteFrom(v.GetSpan());
                            totalCopied += v.Length;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                return totalCopied;
            }
        }

        public int WriteTo(Memory<byte> buffer)
        {
            lock (_syncRoot)
            {
                int nWriteLength = 0;
                nWriteLength = _buffer.WriteTo(buffer.Span);
                return nWriteLength;
            }
        }
    }
}

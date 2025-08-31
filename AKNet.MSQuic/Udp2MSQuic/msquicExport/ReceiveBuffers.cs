

using AKNet.Common;
using System;

namespace AKNet.Udp2MSQuic.Common
{
    internal class ReceiveBuffers
    {
        private const int MaxBufferedBytes = int.MaxValue;
        private readonly object _syncRoot = new object();
        private readonly AkCircularManyBuffer _buffer = new AkCircularManyBuffer();

        public bool HasCapacity()
        {
            lock (_syncRoot)
            {
                return _buffer.Length < MaxBufferedBytes;
            }
        }

        public int CopyFrom(QUIC_BUFFER[] quicBuffers, int BufferCount, int totalLength)
        {
            lock (_syncRoot)
            {
                int totalCopied = 0;
                if (_buffer.Length < MaxBufferedBytes)
                {
                    foreach (var v in quicBuffers)
                    {
                        _buffer.WriteFrom(v.GetSpan());
                        totalCopied += v.Length;
                        if (_buffer.Length > MaxBufferedBytes)
                        {
                            NetLog.LogError("_buffer.Length > MaxBufferedBytes");
                            break;
                        }
                    }
                }
                return totalCopied;
            }
        }

        public int CopyTo(Memory<byte> buffer)
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

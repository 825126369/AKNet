using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AKNet.Udp5Quic.Common
{
    internal struct MsQuicBuffers : IDisposable
    {
        private QUIC_BUFFER[] _buffers;
        private int _count;

        public MsQuicBuffers()
        {
            _buffers = null;
            _count = 0;
        }

        public QUIC_BUFFER[] Buffers => _buffers;
        public int Count => _count;

        private void FreeNativeMemory()
        {
            _buffers = null;
            _count = 0;
        }

        private void Reserve(int count)
        {
            if (count > _count)
            {
                _buffers = new QUIC_BUFFER[count];
                _count = count;
            }
        }

        private void SetBuffer(int index, ReadOnlyMemory<byte> buffer)
        {
            _buffers[index].Buffer = new byte[buffer.Length];
            _buffers[index].Length = buffer.Length;
            buffer.Span.CopyTo(_buffers[index].GetSpan());
        }
        
        public void Initialize<T>(IList<T> inputs, Func<T, ReadOnlyMemory<byte>> toBuffer)
        {
            Reserve(inputs.Count);
            for (int i = 0; i < inputs.Count; ++i)
            {
                SetBuffer(i, toBuffer(inputs[i]));
            }
        }
        
        public void Initialize(ReadOnlyMemory<byte> buffer)
        {
            Reserve(1);
            SetBuffer(0, buffer);
        }
        
        public void Reset()
        {
            for (int i = 0; i < _count; ++i)
            {
                _buffers[i].Buffer = null;
                _buffers[i].Length = 0;
            }
        }
        
        public void Dispose()
        {
            Reset();
            FreeNativeMemory();
        }
    }
}

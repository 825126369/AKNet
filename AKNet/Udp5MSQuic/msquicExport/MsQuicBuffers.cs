using System;
using System.Collections.Generic;

namespace AKNet.Udp5MSQuic.Common
{
    internal struct MsQuicBuffers : IDisposable
    {
        private List<QUIC_BUFFER> _buffers;
        public List<QUIC_BUFFER> Buffers => _buffers;

        public int Count => _buffers.Count;

        private void FreeNativeMemory()
        {
            _buffers = null;
        }

        private void SetBuffer(int index, ReadOnlyMemory<byte> buffer)
        {
            var mBuffer = _buffers[index];
            if (mBuffer == null)
            {
                _buffers[index] = new QUIC_BUFFER();
            }
            
            mBuffer = _buffers[index];
            mBuffer.Buffer = new byte[buffer.Length];
            mBuffer.Length = buffer.Length;
            buffer.Span.CopyTo(mBuffer.GetSpan());
        }
        
        public void Initialize<T>(IList<T> inputs, Func<T, ReadOnlyMemory<byte>> toBuffer)
        {
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

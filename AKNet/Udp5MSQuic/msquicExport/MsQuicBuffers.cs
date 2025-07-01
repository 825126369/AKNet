using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace AKNet.Udp5MSQuic.Common
{
    internal struct MsQuicBuffers : IDisposable
    {
        private QUIC_BUFFER[] _buffers;
        private int _count;
        public QUIC_BUFFER[] Buffers => _buffers;

        public int Count => _count;

        private void FreeNativeMemory()
        {
            _buffers = null;
        }

        private void SetBuffer(int index, ReadOnlyMemory<byte> buffer)
        {
            NetLog.Assert(index < Count);
            _buffers[index] = new QUIC_BUFFER(buffer.Length);
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

        public void Initialize(List<QUIC_BUFFER> mBufferList)
        {
            Reserve(mBufferList.Count);
            _buffers = mBufferList.ToArray();
            _count = mBufferList.Count;
        }

        public void Initialize(ReadOnlyMemory<byte> buffer)
        {
            Reserve(1);
            SetBuffer(0, buffer);
        }
        
        public void Reset()
        {
            for (int i = 0; i < Count; ++i)
            {
                _buffers[i].Buffer = null;
                _buffers[i].Length = 0;
            }
        }

        private void Reserve(int count)
        {
            if (count > _count)
            {
                FreeNativeMemory();
                _buffers = new QUIC_BUFFER[count];
                _count = count;
            }
        }

        public void Dispose()
        {
            Reset();
            FreeNativeMemory();
        }
    }
}

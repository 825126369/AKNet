//using System;
//using System.Collections.Generic;

//namespace AKNet.Udp5MSQuic.Common
//{
//    internal struct MsQuicBuffers : IDisposable
//    {
//        private QUIC_BUFFER[] _buffers;
//        public QUIC_BUFFER[] Buffers => _buffers;

//        public int Count;

//        private void FreeNativeMemory()
//        {
//            _buffers = null;
//        }

//        private void SetBuffer(int index, ReadOnlyMemory<byte> buffer)
//        {
//            if (index >= _buffers)
//            {
//                _buffers.Add(new QUIC_BUFFER());
//            }

//            var mBuffer = _buffers[index];
//            mBuffer = _buffers[index];
//            mBuffer.Buffer = new byte[buffer.Length];
//            mBuffer.Length = buffer.Length;
//            buffer.Span.CopyTo(mBuffer.GetSpan());
//        }
        
//        public void Initialize<T>(IList<T> inputs, Func<T, ReadOnlyMemory<byte>> toBuffer)
//        {
//            for (int i = 0; i < inputs.Count; ++i)
//            {
//                SetBuffer(i, toBuffer(inputs[i]));
//            }
//        }
        
//        public void Initialize(ReadOnlyMemory<byte> buffer)
//        {
//            SetBuffer(0, buffer);
//        }
        
//        public void Reset()
//        {
//            for (int i = 0; i < _buffers.Count; ++i)
//            {
//                _buffers[i].Buffer = null;
//                _buffers[i].Length = 0;
//            }
//        }
        
//        public void Dispose()
//        {
//            Reset();
//            FreeNativeMemory();
//        }
//    }
//}

//using AKNet.Common.Channel;
//using System;
//using System.Buffers;
//using System.IO;
//using System.Runtime.InteropServices;
//using System.Threading;
//using System.Threading.Tasks;

//namespace AKNet.Udp2MSQuic.Common
//{
//    internal partial class QuicStream : Stream
//    {
//        public override bool CanSeek => false;
//        public override long Length => throw new NotSupportedException();
//        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
//        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
//        public override void SetLength(long value) => throw new NotSupportedException();
//        public override bool CanTimeout => true;
//        private TimeSpan _readTimeout = Timeout.InfiniteTimeSpan;
//        private TimeSpan _writeTimeout = Timeout.InfiniteTimeSpan;
//        public override int ReadTimeout
//        {
//            get
//            {
//                if (_disposed > 0)
//                {
//                    throw new ObjectDisposedException(this.ToString());
//                }

//                return (int)_readTimeout.TotalMilliseconds;
//            }
//            set
//            {
//                if (_disposed > 0)
//                {
//                    throw new ObjectDisposedException(this.ToString());
//                }

//                if (value <= 0 && value != Timeout.Infinite)
//                {
//                    throw new ArgumentOutOfRangeException();
//                }
//                _readTimeout = TimeSpan.FromMilliseconds(value);
//            }
//        }
        
//        public override int WriteTimeout
//        {
//            get
//            {
//                if (_disposed > 0)
//                {
//                    throw new ObjectDisposedException(this.ToString());
//                }
//                return (int)_writeTimeout.TotalMilliseconds;
//            }
//            set
//            {
//                if (_disposed > 0)
//                {
//                    throw new ObjectDisposedException(this.ToString());
//                }

//                if (value <= 0 && value != Timeout.Infinite)
//                {
//                    throw new ArgumentOutOfRangeException();
//                }
//                _writeTimeout = TimeSpan.FromMilliseconds(value);
//            }
//        }
        
//        public override bool CanRead => Volatile.Read(ref _disposed) == 0 && _canRead;
//        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
//            => TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, default), callback, state);
        
//        public override int EndRead(IAsyncResult asyncResult)
//            => TaskToAsyncResult.End<int>(asyncResult);
        
//        public override int Read(byte[] buffer, int offset, int count)
//        {
//            return Read(buffer.AsSpan(offset, count));
//        }
        
//        public override int ReadByte()
//        {
//            byte b = 0;
//            return Read(MemoryMarshal.CreateSpan(ref b, 1)) != 0 ? b : -1;
//        }
        
//        public override int Read(Span<byte> buffer)
//        {
//            if (_disposed > 0)
//            {
//                throw new ObjectDisposedException(this.ToString());
//            }

//            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
//            CancellationTokenSource? cts = null;
//            try
//            {
//                if (_readTimeout > TimeSpan.Zero)
//                {
//                    cts = new CancellationTokenSource(_readTimeout);
//                }
//                int readLength = ReadAsync(new Memory<byte>(rentedBuffer, 0, buffer.Length), cts?.Token ?? default).AsTask().GetAwaiter().GetResult();
//                rentedBuffer.AsSpan(0, readLength).CopyTo(buffer);
//                return readLength;
//            }
//            catch (OperationCanceledException) when (cts?.IsCancellationRequested == true)
//            {
//                throw new IOException();
//            }
//            finally
//            {
//                ArrayPool<byte>.Shared.Return(rentedBuffer);
//                cts?.Dispose();
//            }
//        }

//        /// <inheritdoc />
//        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
//        {
//            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
//        }
        
//        public override bool CanWrite => Volatile.Read(ref _disposed) == 0 && _canWrite;
//        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
//            => TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count, default), callback, state);
        
//        public override void EndWrite(IAsyncResult asyncResult)
//            => TaskToAsyncResult.End(asyncResult);

        
//        public override void Write(byte[] buffer, int offset, int count)
//        {
//            Write(buffer.AsSpan(offset, count));
//        }
        
//        public override void WriteByte(byte value)
//        {
//            Write(MemoryMarshal.CreateSpan(ref value, 1));
//        }
        
//        public override void Write(ReadOnlySpan<byte> buffer)
//        {
//            if (_disposed > 0)
//            {
//                throw new ObjectDisposedException(this.ToString());
//            }

//            CancellationTokenSource? cts = null;
//            if (_writeTimeout > TimeSpan.Zero)
//            {
//                cts = new CancellationTokenSource(_writeTimeout);
//            }
//            try
//            {
//                WriteAsync(buffer.ToArray(), cts?.Token ?? default).AsTask().GetAwaiter().GetResult();
//            }
//            catch (OperationCanceledException) when (cts?.IsCancellationRequested == true)
//            {
//                throw new IOException();
//            }
//            finally
//            {
//                cts?.Dispose();
//            }
//        }
        
//        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
//        {
//            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
//        }
        
//        public override void Flush() => FlushAsync().GetAwaiter().GetResult();
        
//        public override Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        
//        protected override void Dispose(bool disposing)
//        {
//            DisposeAsync().AsTask().GetAwaiter().GetResult();
//            base.Dispose(disposing);
//        }
//    }
//}

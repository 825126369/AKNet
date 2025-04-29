using System;
using System.IO;

namespace AKNet.Udp5Quic.Common
{
    public sealed class QuicException : IOException
    {
        public QuicException(QuicError error, long? applicationErrorCode, string message)
            : this(error, applicationErrorCode, null, message, null)
        { }
        
        internal QuicException(QuicError error, long? applicationErrorCode, long? transportErrorCode, string message)
            : this(error, applicationErrorCode, transportErrorCode, message, null)
        { }
        
        internal QuicException(QuicError error, long? applicationErrorCode, string message, Exception? innerException)
            : this(error, applicationErrorCode, null, message, innerException)
        { }
        
        internal QuicException(QuicError error, long? applicationErrorCode, long? transportErrorCode, string message, Exception? innerException)
            : base(message, innerException)
        {
            QuicError = error;
            ApplicationErrorCode = applicationErrorCode;
            TransportErrorCode = transportErrorCode;
        }
        
        public QuicError QuicError { get; }
        
        public long? ApplicationErrorCode { get; }
        
        public long? TransportErrorCode { get; }
    }
}

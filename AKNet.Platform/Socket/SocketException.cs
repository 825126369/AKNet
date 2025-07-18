using System.ComponentModel;

namespace AKNet.Platform.Socket
{
    public partial class SocketException : Win32Exception
    {
        private readonly SocketError _errorCode;
        public SocketException(int errorCode) : this((SocketError)errorCode)
        {
            
        }

        public SocketException(int errorCode, string? message) : this((SocketError)errorCode, message)
        {

        }

        internal SocketException(SocketError socketError) : base(GetNativeErrorForSocketError(socketError))
        {
            _errorCode = socketError;
        }

        internal SocketException(SocketError socketError, string? message) : base(GetNativeErrorForSocketError(socketError), message)
        {
            _errorCode = socketError;
        }

        public override string Message => base.Message;

        public SocketError SocketErrorCode => (SocketError)_errorCode;
        public override int ErrorCode => base.NativeErrorCode;
    }
}

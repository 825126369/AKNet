using System.ComponentModel;
using System.Runtime.Serialization;

namespace AKNet.Socket
{
    [Serializable]
    public partial class SocketException : Win32Exception
    {
        private readonly SocketError _errorCode;
        public SocketException(int errorCode) : this((SocketError)errorCode)
        {
            
        }

        /// <summary>Initializes a new instance of the <see cref='System.Net.Sockets.SocketException'/> class with the specified error code and optional message.</summary>
        public SocketException(int errorCode, string? message) : this((SocketError)errorCode, message)
        {
        }

        /// <summary>Creates a new instance of the <see cref='System.Net.Sockets.SocketException'/> class with the specified error code as SocketError.</summary>
        internal SocketException(SocketError socketError) : base(GetNativeErrorForSocketError(socketError))
        {
            _errorCode = socketError;
        }

        /// <summary>Initializes a new instance of the <see cref='System.Net.Sockets.SocketException'/> class with the specified error code as SocketError and optional message.</summary>
        internal SocketException(SocketError socketError, string? message) : base(GetNativeErrorForSocketError(socketError), message)
        {
            _errorCode = socketError;
        }

        public override string Message => base.Message;

        public SocketError SocketErrorCode => (SocketError)_errorCode;
        public override int ErrorCode => base.NativeErrorCode;
    }
}

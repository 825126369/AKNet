using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    public partial class SocketException : Win32Exception
    {
        /// <summary>Creates a new instance of the <see cref='System.Net.Sockets.SocketException'/> class with the default error code.</summary>
        public SocketException() : this(Marshal.GetLastWin32Error())
        {

        }

        private static int GetNativeErrorForSocketError(SocketError error)
        {
            return (int)error;
        }
    }
}

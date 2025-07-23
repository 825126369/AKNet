using System.Net.Sockets;

namespace AKNet.Common
{
    internal class SSocketAsyncEventArgs: SocketAsyncEventArgs
    {
        public void Do()
        {
            this.OnCompleted(this);
        }
    }
}

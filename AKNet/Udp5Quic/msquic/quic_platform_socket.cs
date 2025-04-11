using System.Net;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_ADDR
    {
        public IPAddress Ip;
        public int nPort;

        public IPEndPoint GetIPEndPoint()
        {
            return new IPEndPoint(Ip, nPort);
        }
    }

    internal static partial class MSQuicFunc
    {

    }
}

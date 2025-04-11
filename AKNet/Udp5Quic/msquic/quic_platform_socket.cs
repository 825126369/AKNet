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

        public void WriteTo(byte[] Buffer)
        {
            
        }

        public void WriteFrom(byte[] Buffer)
        {

        }
    }

    internal static partial class MSQuicFunc
    {

    }
}

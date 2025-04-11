using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_ADDR
    {
        public AddressFamily AddressFamily;
        public IPAddress Ip;
        public int nPort;

        public IPEndPoint GetIPEndPoint()
        {
            return new IPEndPoint(Ip, nPort);
        }

        public QUIC_ADDR MapToIPv6()
        {
            QUIC_ADDR OutAddr = new QUIC_ADDR();
            OutAddr.nPort = nPort;
            OutAddr.AddressFamily = AddressFamily.InterNetworkV6;
            if (AddressFamily == AddressFamily.InterNetwork)
            {
                OutAddr.Ip = Ip.MapToIPv6();
            }
            else
            {
                OutAddr.Ip = Ip;
            }

            return OutAddr;
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

using AKNet.Common;
using System.Net;
using System.Net.Sockets;
#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.Udp1MSQuic.Common
{
    internal static class MsQuicHelpers
    {
        internal static bool TryParse(this EndPoint endPoint, out string? host, out IPAddress? address, out int port)
        {
            if (endPoint is DnsEndPoint dnsEndPoint)
            {
                host = IPAddress.TryParse(dnsEndPoint.Host, out address) ? null : dnsEndPoint.Host;
                port = dnsEndPoint.Port;
                return true;
            }

            if (endPoint is IPEndPoint ipEndPoint)
            {
                host = null;
                address = ipEndPoint.Address;
                port = ipEndPoint.Port;
                return true;
            }

            host = default;
            address = default;
            port = default;
            return false;
        }

        public static QUIC_SSBuffer GetMsQuicParameter(QUIC_HANDLE handle, uint parameter)
        {
            QUIC_SSBuffer value = default;
            int status = MSQuicFunc.MsQuicGetParam(handle, parameter, value);
            if (MSQuicFunc.QUIC_FAILED(status))
            {
                NetLog.LogError($"GetParam({handle}, {parameter}) failed");
            }
            return value;
        }

        public static void SetMsQuicParameter(QUIC_HANDLE handle, uint parameter, QUIC_SSBuffer value)
        {
            int status = MSQuicFunc.MsQuicSetParam(handle, parameter, value);
            if (MSQuicFunc.QUIC_FAILED(status))
            {
                NetLog.LogError($"SetParam({handle}, {parameter}) failed");
            }
        }

    }
}

using AKNet.Common;
using System.Net;

#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.Udp2MSQuic.Common
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

        public static QUIC_ADDR ToQuicAddr(this IPEndPoint ipEndPoint)
        {
            QUIC_ADDR result = new QUIC_ADDR(ipEndPoint);
            return result;
        }

        internal static T GetMsQuicParameter<T>(QUIC_HANDLE handle, uint parameter)
        {
            //T value;
            //GetMsQuicParameter(handle, parameter, (uint)sizeof(T), (byte*)&value);
            //return value;
            return default;
        }

        public static void GetMsQuicParameter(QUIC_HANDLE handle, uint parameter, QUIC_SSBuffer value)
        {
            int status = MSQuicFunc.MsQuicGetParam(handle, parameter, value);
            if (MSQuicFunc.QUIC_FAILED(status))
            {
                NetLog.LogError($"GetParam({handle}, {parameter}) failed");
            }
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

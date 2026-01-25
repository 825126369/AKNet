/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:20
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Net;

#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.MSQuic.Common
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

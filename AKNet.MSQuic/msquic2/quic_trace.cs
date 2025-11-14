/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:48
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace MSQuic2
{
    internal enum QUIC_TRACE_API_TYPE
    {
        QUIC_TRACE_API_SET_PARAM,
        QUIC_TRACE_API_GET_PARAM,
        QUIC_TRACE_API_REGISTRATION_OPEN,
        QUIC_TRACE_API_REGISTRATION_CLOSE,
        QUIC_TRACE_API_REGISTRATION_SHUTDOWN,
        QUIC_TRACE_API_CONFIGURATION_OPEN,
        QUIC_TRACE_API_CONFIGURATION_CLOSE,
        QUIC_TRACE_API_CONFIGURATION_LOAD_CREDENTIAL,
        QUIC_TRACE_API_LISTENER_OPEN,
        QUIC_TRACE_API_LISTENER_CLOSE,
        QUIC_TRACE_API_LISTENER_START,
        QUIC_TRACE_API_LISTENER_STOP,
        QUIC_TRACE_API_CONNECTION_OPEN,
        QUIC_TRACE_API_CONNECTION_CLOSE,
        QUIC_TRACE_API_CONNECTION_SHUTDOWN,
        QUIC_TRACE_API_CONNECTION_START,
        QUIC_TRACE_API_CONNECTION_SET_CONFIGURATION,
        QUIC_TRACE_API_CONNECTION_SEND_RESUMPTION_TICKET,
        QUIC_TRACE_API_STREAM_OPEN,
        QUIC_TRACE_API_STREAM_CLOSE,
        QUIC_TRACE_API_STREAM_START,
        QUIC_TRACE_API_STREAM_SHUTDOWN,
        QUIC_TRACE_API_STREAM_SEND,
        QUIC_TRACE_API_STREAM_RECEIVE_COMPLETE,
        QUIC_TRACE_API_STREAM_RECEIVE_SET_ENABLED,
        QUIC_TRACE_API_DATAGRAM_SEND,
        QUIC_TRACE_API_CONNECTION_COMPLETE_RESUMPTION_TICKET_VALIDATION,
        QUIC_TRACE_API_CONNECTION_COMPLETE_CERTIFICATE_VALIDATION,
        QUIC_TRACE_API_STREAM_PROVIDE_RECEIVE_BUFFERS,
        QUIC_TRACE_API_COUNT // Must be last
    }

    internal static partial class MSQuicFunc
    {
        internal delegate void QUIC_TRACE_RUNDOWN_CALLBACK();
        public static void QuicTraceLogVerbose(string log)
        {
            
        }
    }
}

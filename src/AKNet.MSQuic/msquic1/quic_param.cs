/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:18
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
namespace MSQuic1
{
    internal static partial class MSQuicFunc
    {
        static int QuicLibrarySetGlobalParam(uint Param, ReadOnlySpan<byte> Buffer)
        {
            int Status = QUIC_STATUS_SUCCESS;
            QUIC_SETTINGS InternalSettings = new QUIC_SETTINGS();

            switch (Param)
            {
                case QUIC_PARAM_GLOBAL_RETRY_MEMORY_PERCENT:
                    Status = QUIC_STATUS_SUCCESS;
                    break;
                case QUIC_PARAM_GLOBAL_LOAD_BALACING_MODE:
                    break;
                case QUIC_PARAM_GLOBAL_SETTINGS:
                    break;
                case QUIC_PARAM_GLOBAL_GLOBAL_SETTINGS:
                    break;
                case QUIC_PARAM_GLOBAL_VERSION_SETTINGS:
                    break;
                case QUIC_PARAM_GLOBAL_EXECUTION_CONFIG:
                    break;
                case QUIC_PARAM_GLOBAL_STATELESS_RESET_KEY:
                    break;

                default:
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
            }

            return Status;
        }

        static int QuicRegistrationParamSet(QUIC_REGISTRATION Registration, uint Param, ReadOnlySpan<byte> Buffer)
        {
            return QUIC_STATUS_INVALID_PARAMETER;
        }

        static int QuicRegistrationParamGet(QUIC_REGISTRATION Registration, uint Param, QUIC_SSBuffer Buffer)
        {
            return QUIC_STATUS_INVALID_PARAMETER;
        }

        static int QuicListenerParamSet(QUIC_LISTENER Listener, uint Param, ReadOnlySpan<byte> Buffer)
        {
            return QUIC_STATUS_INVALID_PARAMETER;
        }

        static int QuicListenerParamGet(QUIC_LISTENER Listener, uint Param, QUIC_BUFFER Buffer)
        {
            int Status = QUIC_STATUS_SUCCESS;
            return Status;
        }

        static int QuicConnParamSet(QUIC_CONNECTION Connection, uint Param, ReadOnlySpan<byte> Buffer)
        {
            int Status;
            QUIC_SETTINGS InternalSettings = new QUIC_SETTINGS();
            switch (Param)
            {
                default:
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
            }

            return Status;
        }

        static int CxPlatTlsParamSet(CXPLAT_TLS SecConfig, uint Param, ReadOnlySpan<byte> Buffer)
        {
            return QUIC_STATUS_NOT_SUPPORTED;
        }

        static int QuicStreamParamSet(QUIC_STREAM Stream, uint Param, ReadOnlySpan<byte> Buffer)
        {
            return QUIC_STATUS_NOT_SUPPORTED;
        }

        static int QuicConfigurationParamSet(QUIC_CONFIGURATION Configuration, uint Param, ReadOnlySpan<byte> Buffer)
        {
            return QUIC_STATUS_NOT_SUPPORTED;
        }
    }
}

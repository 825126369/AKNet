using AKNet.Common;
using System.Diagnostics;

namespace AKNet.Udp5Quic.Common
{
    internal sealed class MsQuicApi
    {
        public static MsQuicApi Api = new MsQuicApi();
        public QUIC_REGISTRATION Registration;

        internal static string MsQuicLibraryVersion { get; } = "unknown";
        internal static string? NotSupportedReason { get; }
        internal static bool UsesSChannelBackend { get; }
        internal static bool Tls13ServerMayBeDisabled { get; }
        internal static bool Tls13ClientMayBeDisabled { get; }


        private MsQuicApi()
        {
            var cfg = new QUIC_REGISTRATION_CONFIG
            {
                AppName = "AKNet.Quic",
                ExecutionProfile = QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_LOW_LATENCY
            };

            if (MsQuicHelpers.QUIC_FAILED(MSQuicFunc.MsQuicRegistrationOpen(cfg, ref Registration)))
            {
                NetLog.LogError("MsQuicRegistrationOpen Fail");
            }
        }

        private static MsQuicApi AllocateMsQuicApi()
        {
            return new MsQuicApi();
        }

        private static bool TryOpenMsQuic(out QUIC_API_TABLE* apiTable, out int openStatus)
        {
            Debug.Assert(MsQuicOpenVersion != null);

            QUIC_API_TABLE* table = null;
            openStatus = MSQuicFunc.MS((uint)s_minMsQuicVersion.Major, &table);
            if (StatusFailed(openStatus))
            {
                apiTable = null;
                return false;
            }

            apiTable = table;
            return true;
        }


    }
}

using AKNet.Common;
using System;

namespace AKNet.Udp5MSQuic.Common
{
    internal sealed class MsQuicApi
    {
        private static readonly Lazy<MsQuicApi> _api = new Lazy<MsQuicApi>(AllocateMsQuicApi);
        public QUIC_REGISTRATION Registration;

        internal static string MsQuicLibraryVersion { get; } = "unknown";
        internal static string? NotSupportedReason { get; }
        internal static bool UsesSChannelBackend { get; }
        internal static bool Tls13ServerMayBeDisabled { get; }
        internal static bool Tls13ClientMayBeDisabled { get; }

        private static readonly Version s_minWindowsVersion = new Version(10, 0, 20145, 1000);
        private static readonly Version s_minMsQuicVersion = new Version(2, 2, 2);
        
        public static MsQuicApi Api
        {
            get { return _api.Value; }
        }

        private MsQuicApi()
        {
            MSQuicFunc.DoTest();
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
            TryOpenMsQuic(out QUIC_API_TABLE apiTable, out int openStatus);
            return new MsQuicApi();
        }

        private static bool TryOpenMsQuic(out QUIC_API_TABLE apiTable, out int openStatus)
        {
            apiTable = null;
            openStatus = MSQuicFunc.MsQuicOpenVersion((uint)s_minMsQuicVersion.Major, out apiTable);
            if (MsQuicHelpers.QUIC_FAILED(openStatus))
            {
                NetLog.LogError("MSQuicFunc.MsQuicOpenVersion Error");
                return false;
            }
            return true;
        }


    }
}

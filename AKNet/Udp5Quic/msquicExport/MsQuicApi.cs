namespace AKNet.Udp5Quic.Common
{
    internal sealed class MsQuicApi
    {
        public static MsQuicApi Api = new MsQuicApi();
        public QUIC_REGISTRATION Registration;

        private MsQuicApi()
        {
            var cfg = new QUIC_REGISTRATION_CONFIG
            {
                AppName = "AKNet.Quic",
                ExecutionProfile = QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_LOW_LATENCY
            };

            MSQuicFunc.MsQuicRegistrationOpen(cfg, ref Registration);
        }

        internal static bool IsQuicSupported { get; }

        internal static string MsQuicLibraryVersion { get; } = "unknown";
        internal static string? NotSupportedReason { get; }
        internal static bool UsesSChannelBackend { get; }
        internal static bool Tls13ServerMayBeDisabled { get; }
        internal static bool Tls13ClientMayBeDisabled { get; }

        static MsQuicApi()
        {
            IsQuicSupported = true;
        }


    }
}

using AKNet.Common;
using System;

namespace AKNet.Udp1MSQuic.Common
{
    internal sealed class MsQuicApi
    {
        private static readonly Lazy<MsQuicApi> _lazyInstance = new Lazy<MsQuicApi>(() => new MsQuicApi()); //线程安全
        public QUIC_REGISTRATION Registration;
        private static readonly Version s_minMsQuicVersion = new Version(2, 0, 0);
        private static bool bInit = false;

        public static MsQuicApi Api => _lazyInstance.Value;

        public MsQuicApi()
        {
            CheckAndInit();
        }


        private bool CheckAndInit()
        {
            if(bInit)
            {
                NetLog.LogError("单例 有问题");
                return false;
            }
            bInit = true;

            //MSQuicFunc.DoTest();
            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicOpenVersion((uint)s_minMsQuicVersion.Major, out _)))
            {
                NetLog.LogError("MSQuicFunc.MsQuicOpenVersion Error");
                return false;
            }

            var cfg = new QUIC_REGISTRATION_CONFIG
            {
                AppName = "AKNet.Quic",
                ExecutionProfile = QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_LOW_LATENCY
            };

            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicRegistrationOpen(cfg, out Registration)))
            {
                NetLog.LogError("MsQuicRegistrationOpen Fail");
                return false;
            }
            return true;
        }
    }
}

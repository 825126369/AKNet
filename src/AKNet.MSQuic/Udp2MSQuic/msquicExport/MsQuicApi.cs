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
using System;

#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.Udp2MSQuic.Common
{
    internal sealed class MsQuicApi
    {
        private static readonly Lazy<MsQuicApi> _lazyInstance = new Lazy<MsQuicApi>(() => new MsQuicApi()); //�̰߳�ȫ
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
            if (bInit)
            {
                NetLog.LogError("���� ������");
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

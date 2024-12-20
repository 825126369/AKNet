/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:52
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp
{
    //管理信息库
    internal class netns_mib
    {
        public linux_mib net_statistics = new linux_mib();
        public tcp_mib tcp_statistics = new tcp_mib();
    }

    internal class linux_mib
    {
        public long[] mibs = new long[(int)LINUXMIB.__LINUX_MIB_MAX];
    };

    internal class tcp_mib
    {
        public long[] mibs = new long[(int)TCPMIB.__TCP_MIB_MAX];
    };



    internal enum TCPMIB
    {
        TCP_MIB_NUM = 0,
        TCP_MIB_RTOALGORITHM,           /* RtoAlgorithm */
        TCP_MIB_RTOMIN,             /* RtoMin */
        TCP_MIB_RTOMAX,             /* RtoMax */
        TCP_MIB_MAXCONN,            /* MaxConn */
        TCP_MIB_ACTIVEOPENS,            /* ActiveOpens */
        TCP_MIB_PASSIVEOPENS,           /* PassiveOpens */
        TCP_MIB_ATTEMPTFAILS,           /* AttemptFails */
        TCP_MIB_ESTABRESETS,            /* EstabResets */
        TCP_MIB_CURRESTAB,          /* CurrEstab */
        TCP_MIB_INSEGS,             /* InSegs */
        TCP_MIB_OUTSEGS,            /* OutSegs */
        TCP_MIB_RETRANSSEGS,            /* RetransSegs */
        TCP_MIB_INERRS,             /* InErrs */
        TCP_MIB_OUTRSTS,            /* OutRsts */
        TCP_MIB_CSUMERRORS,         /* InCsumErrors */
        __TCP_MIB_MAX
    };

    /* linux mib definitions */
    public enum LINUXMIB
    {
        LINUX_MIB_NUM = 0,
        LINUX_MIB_SYNCOOKIESSENT,       /* SyncookiesSent */
        LINUX_MIB_SYNCOOKIESRECV,       /* SyncookiesRecv */
        LINUX_MIB_SYNCOOKIESFAILED,     /* SyncookiesFailed */
        LINUX_MIB_EMBRYONICRSTS,        /* EmbryonicRsts */
        LINUX_MIB_PRUNECALLED,          /* PruneCalled */
        LINUX_MIB_RCVPRUNED,            /* RcvPruned */
        LINUX_MIB_OFOPRUNED,            /* OfoPruned */
        LINUX_MIB_OUTOFWINDOWICMPS,     /* OutOfWindowIcmps */
        LINUX_MIB_LOCKDROPPEDICMPS,     /* LockDroppedIcmps */
        LINUX_MIB_ARPFILTER,            /* ArpFilter */
        LINUX_MIB_TIMEWAITED,           /* TimeWaited */
        LINUX_MIB_TIMEWAITRECYCLED,     /* TimeWaitRecycled */
        LINUX_MIB_TIMEWAITKILLED,       /* TimeWaitKilled */
        LINUX_MIB_PAWSACTIVEREJECTED,       /* PAWSActiveRejected */
        LINUX_MIB_PAWSESTABREJECTED,        /* PAWSEstabRejected */
        LINUX_MIB_DELAYEDACKS,          /* DelayedACKs */
        LINUX_MIB_DELAYEDACKLOCKED,     /* DelayedACKLocked */
        LINUX_MIB_DELAYEDACKLOST,       /* DelayedACKLost */
        LINUX_MIB_LISTENOVERFLOWS,      /* ListenOverflows */
        LINUX_MIB_LISTENDROPS,          /* ListenDrops */
        LINUX_MIB_TCPHPHITS,            /* TCPHPHits */
        LINUX_MIB_TCPPUREACKS,          /* TCPPureAcks */
        LINUX_MIB_TCPHPACKS,            /* TCPHPAcks */
        LINUX_MIB_TCPRENORECOVERY,      /* TCPRenoRecovery */
        LINUX_MIB_TCPSACKRECOVERY,      /* TCPSackRecovery */
        LINUX_MIB_TCPSACKRENEGING,      /* TCPSACKReneging */
        LINUX_MIB_TCPSACKREORDER,       /* TCPSACKReorder */
        LINUX_MIB_TCPRENOREORDER,       /* TCPRenoReorder */
        LINUX_MIB_TCPTSREORDER,         /* TCPTSReorder */
        LINUX_MIB_TCPFULLUNDO,          /* TCPFullUndo */
        LINUX_MIB_TCPPARTIALUNDO,       /* TCPPartialUndo */
        LINUX_MIB_TCPDSACKUNDO,         /* TCPDSACKUndo */
        LINUX_MIB_TCPLOSSUNDO,          /* TCPLossUndo */
        LINUX_MIB_TCPLOSTRETRANSMIT,        /* TCPLostRetransmit */
        LINUX_MIB_TCPRENOFAILURES,      /* TCPRenoFailures */
        LINUX_MIB_TCPSACKFAILURES,      /* TCPSackFailures */
        LINUX_MIB_TCPLOSSFAILURES,      /* TCPLossFailures */
        LINUX_MIB_TCPFASTRETRANS,       /* TCPFastRetrans */
        LINUX_MIB_TCPSLOWSTARTRETRANS,      /* TCPSlowStartRetrans */
        LINUX_MIB_TCPTIMEOUTS,          /* TCPTimeouts */
        LINUX_MIB_TCPLOSSPROBES,        /* TCPLossProbes */
        LINUX_MIB_TCPLOSSPROBERECOVERY,     /* TCPLossProbeRecovery */
        LINUX_MIB_TCPRENORECOVERYFAIL,      /* TCPRenoRecoveryFail */
        LINUX_MIB_TCPSACKRECOVERYFAIL,      /* TCPSackRecoveryFail */
        LINUX_MIB_TCPRCVCOLLAPSED,      /* TCPRcvCollapsed */
        LINUX_MIB_TCPDSACKOLDSENT,      /* TCPDSACKOldSent */
        LINUX_MIB_TCPDSACKOFOSENT,      /* TCPDSACKOfoSent */
        LINUX_MIB_TCPDSACKRECV,         /* TCPDSACKRecv */
        LINUX_MIB_TCPDSACKOFORECV,      /* TCPDSACKOfoRecv */
        LINUX_MIB_TCPABORTONDATA,       /* TCPAbortOnData */
        LINUX_MIB_TCPABORTONCLOSE,      /* TCPAbortOnClose */
        LINUX_MIB_TCPABORTONMEMORY,     /* TCPAbortOnMemory */
        LINUX_MIB_TCPABORTONTIMEOUT,        /* TCPAbortOnTimeout */
        LINUX_MIB_TCPABORTONLINGER,     /* TCPAbortOnLinger */
        LINUX_MIB_TCPABORTFAILED,       /* TCPAbortFailed */
        LINUX_MIB_TCPMEMORYPRESSURES,       /* TCPMemoryPressures */
        LINUX_MIB_TCPMEMORYPRESSURESCHRONO, /* TCPMemoryPressuresChrono */
        LINUX_MIB_TCPSACKDISCARD,       /* TCPSACKDiscard */
        LINUX_MIB_TCPDSACKIGNOREDOLD,       /* TCPSACKIgnoredOld */
        LINUX_MIB_TCPDSACKIGNOREDNOUNDO,    /* TCPSACKIgnoredNoUndo */
        LINUX_MIB_TCPSPURIOUSRTOS,      /* TCPSpuriousRTOs */
        LINUX_MIB_TCPMD5NOTFOUND,       /* TCPMD5NotFound */
        LINUX_MIB_TCPMD5UNEXPECTED,     /* TCPMD5Unexpected */
        LINUX_MIB_TCPMD5FAILURE,        /* TCPMD5Failure */
        LINUX_MIB_SACKSHIFTED,
        LINUX_MIB_SACKMERGED,
        LINUX_MIB_SACKSHIFTFALLBACK,
        LINUX_MIB_TCPBACKLOGDROP,
        LINUX_MIB_PFMEMALLOCDROP,
        LINUX_MIB_TCPMINTTLDROP, /* RFC 5082 */
        LINUX_MIB_TCPDEFERACCEPTDROP,
        LINUX_MIB_IPRPFILTER, /* IP Reverse Path Filter (rp_filter) */
        LINUX_MIB_TCPTIMEWAITOVERFLOW,      /* TCPTimeWaitOverflow */
        LINUX_MIB_TCPREQQFULLDOCOOKIES,     /* TCPReqQFullDoCookies */
        LINUX_MIB_TCPREQQFULLDROP,      /* TCPReqQFullDrop */
        LINUX_MIB_TCPRETRANSFAIL,       /* TCPRetransFail */
        LINUX_MIB_TCPRCVCOALESCE,       /* TCPRcvCoalesce */
        LINUX_MIB_TCPBACKLOGCOALESCE,       /* TCPBacklogCoalesce */
        LINUX_MIB_TCPOFOQUEUE,          /* TCPOFOQueue */
        LINUX_MIB_TCPOFODROP,           /* TCPOFODrop */
        LINUX_MIB_TCPOFOMERGE,          /* TCPOFOMerge */
        LINUX_MIB_TCPCHALLENGEACK,      /* TCPChallengeACK */
        LINUX_MIB_TCPSYNCHALLENGE,      /* TCPSYNChallenge */
        LINUX_MIB_TCPFASTOPENACTIVE,        /* TCPFastOpenActive */
        LINUX_MIB_TCPFASTOPENACTIVEFAIL,    /* TCPFastOpenActiveFail */
        LINUX_MIB_TCPFASTOPENPASSIVE,       /* TCPFastOpenPassive*/
        LINUX_MIB_TCPFASTOPENPASSIVEFAIL,   /* TCPFastOpenPassiveFail */
        LINUX_MIB_TCPFASTOPENLISTENOVERFLOW,    /* TCPFastOpenListenOverflow */
        LINUX_MIB_TCPFASTOPENCOOKIEREQD,    /* TCPFastOpenCookieReqd */
        LINUX_MIB_TCPFASTOPENBLACKHOLE,     /* TCPFastOpenBlackholeDetect */
        LINUX_MIB_TCPSPURIOUS_RTX_HOSTQUEUES, /* TCPSpuriousRtxHostQueues */
        LINUX_MIB_BUSYPOLLRXPACKETS,        /* BusyPollRxPackets */
        LINUX_MIB_TCPAUTOCORKING,       /* TCPAutoCorking */
        LINUX_MIB_TCPFROMZEROWINDOWADV,     /* TCPFromZeroWindowAdv */
        LINUX_MIB_TCPTOZEROWINDOWADV,       /* TCPToZeroWindowAdv */
        LINUX_MIB_TCPWANTZEROWINDOWADV,     /* TCPWantZeroWindowAdv */
        LINUX_MIB_TCPSYNRETRANS,        /* TCPSynRetrans */
        LINUX_MIB_TCPORIGDATASENT,      /* TCPOrigDataSent */
        LINUX_MIB_TCPHYSTARTTRAINDETECT,    /* TCPHystartTrainDetect */
        LINUX_MIB_TCPHYSTARTTRAINCWND,      /* TCPHystartTrainCwnd */
        LINUX_MIB_TCPHYSTARTDELAYDETECT,    /* TCPHystartDelayDetect */
        LINUX_MIB_TCPHYSTARTDELAYCWND,      /* TCPHystartDelayCwnd */
        LINUX_MIB_TCPACKSKIPPEDSYNRECV,     /* TCPACKSkippedSynRecv */
        LINUX_MIB_TCPACKSKIPPEDPAWS,        /* TCPACKSkippedPAWS */
        LINUX_MIB_TCPACKSKIPPEDSEQ,     /* TCPACKSkippedSeq */
        LINUX_MIB_TCPACKSKIPPEDFINWAIT2,    /* TCPACKSkippedFinWait2 */
        LINUX_MIB_TCPACKSKIPPEDTIMEWAIT,    /* TCPACKSkippedTimeWait */
        LINUX_MIB_TCPACKSKIPPEDCHALLENGE,   /* TCPACKSkippedChallenge */
        LINUX_MIB_TCPWINPROBE,          /* TCPWinProbe */
        LINUX_MIB_TCPKEEPALIVE,         /* TCPKeepAlive */
        LINUX_MIB_TCPMTUPFAIL,          /* TCPMTUPFail */
        LINUX_MIB_TCPMTUPSUCCESS,       /* TCPMTUPSuccess */
        LINUX_MIB_TCPDELIVERED,         /* TCPDelivered */
        LINUX_MIB_TCPDELIVEREDCE,       /* TCPDeliveredCE */
        LINUX_MIB_TCPACKCOMPRESSED,     /* TCPAckCompressed */
        LINUX_MIB_TCPZEROWINDOWDROP,        /* TCPZeroWindowDrop */
        LINUX_MIB_TCPRCVQDROP,          /* TCPRcvQDrop */
        LINUX_MIB_TCPWQUEUETOOBIG,      /* TCPWqueueTooBig */
        LINUX_MIB_TCPFASTOPENPASSIVEALTKEY, /* TCPFastOpenPassiveAltKey */
        LINUX_MIB_TCPTIMEOUTREHASH,     /* TCPTimeoutRehash */
        LINUX_MIB_TCPDUPLICATEDATAREHASH,   /* TCPDuplicateDataRehash */
        LINUX_MIB_TCPDSACKRECVSEGS,     /* TCPDSACKRecvSegs */
        LINUX_MIB_TCPDSACKIGNOREDDUBIOUS,   /* TCPDSACKIgnoredDubious */
        LINUX_MIB_TCPMIGRATEREQSUCCESS,     /* TCPMigrateReqSuccess */
        LINUX_MIB_TCPMIGRATEREQFAILURE,     /* TCPMigrateReqFailure */
        LINUX_MIB_TCPPLBREHASH,         /* TCPPLBRehash */
        LINUX_MIB_TCPAOREQUIRED,        /* TCPAORequired */
        LINUX_MIB_TCPAOBAD,         /* TCPAOBad */
        LINUX_MIB_TCPAOKEYNOTFOUND,     /* TCPAOKeyNotFound */
        LINUX_MIB_TCPAOGOOD,            /* TCPAOGood */
        LINUX_MIB_TCPAODROPPEDICMPS,        /* TCPAODroppedIcmps */
        __LINUX_MIB_MAX
    };

}

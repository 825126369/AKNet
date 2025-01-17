/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp
{
    internal enum TCP_MIB
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
    }
}


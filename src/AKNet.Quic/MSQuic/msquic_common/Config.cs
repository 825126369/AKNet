/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:02
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.MSQuic.Common
{
    internal static class Config
    {
        //Common
        public const int nIOContexBufferLength = 1024;
        public const int nDataMaxLength = ushort.MaxValue;
        public const double fReceiveHeartBeatTimeOut = 5.0;
        public const double fMySendHeartBeatMaxTime = 2.0;
        public const double fReConnectMaxCdTime = 3.0;
        public const int MaxPlayerCount = 10000;

        public const int DefaultCloseErrorCode = byte.MaxValue;
        public const int DefaultStreamErrorCode = ushort.MaxValue;
    }
}

/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:17
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4Tcp.Common
{
    internal class ReSendPackageTimeOut
    {
        long fLastTime = 0;
        long fInternalTime = -1;
        public void SetInternalTime(long fNowTime, long fInternalTime)
        {
            this.Reset(fNowTime);
            this.fInternalTime = fInternalTime;
        }

        public void Reset(long mNowTime)
        {
            this.fInternalTime = -1;
            this.fLastTime = mNowTime;
        }

        public bool orSetInternalTime()
        {
            return fInternalTime >= 0.0;
        }

        public bool orTimeOut(long fNowTime)
        {
            if (fNowTime - fLastTime >= fInternalTime)
            {
                this.Reset(fNowTime);
                return true;
            }

            return false;
        }
    }
}
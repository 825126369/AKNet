/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp5Tcp.Common
{
    internal class ReSendPackageTimeOut
    {
        long fLastTime = 0;
        long fInternalTime = -1;
        private LogicWorker mLogicWorker;
        public void SetInternalTime(long fInternalTime)
        {
            this.Reset();
            this.fInternalTime = fInternalTime;
        }

        public void Reset()
        {
            this.fInternalTime = -1;
            this.fLastTime = mLogicWorker.mThreadWorker.TimeNow;
        }

        public bool orSetInternalTime()
        {
            return fInternalTime >= 0.0;
        }

        public bool orTimeOut()
        {
            if (mLogicWorker.mThreadWorker.TimeNow - fLastTime >= fInternalTime)
            {
                this.Reset();
                return true;
            }

            return false;
        }

        public void SetLogicWorker(LogicWorker mLogicWorker)
        {
            this.mLogicWorker = mLogicWorker;
        }
    }
}
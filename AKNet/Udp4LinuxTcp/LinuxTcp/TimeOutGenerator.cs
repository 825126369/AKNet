/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4LinuxTcp
{
    internal class TimeOutGenerator
    {
        double fTimeOutTime = 0.0;
        public void SetInternalTime(double fTimeOutTime)
        {
            this.Reset();
            this.fTimeOutTime = fTimeOutTime;
        }

        public void Reset()
        {
            this.fTimeOutTime = 0.0;
        }

        public bool orTimeOut(double fElapsed)
        {
            if (this.fTimeOutTime <= 0.0) { return false; }
            if (LinuxTcpFunc.tcp_jiffies32 >= fTimeOutTime)
            {
                this.Reset();
                return true;
            }

            return false;
        }
    }
}
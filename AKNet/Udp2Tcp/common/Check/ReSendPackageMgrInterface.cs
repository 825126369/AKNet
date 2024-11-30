/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp2Tcp.Common
{
    internal class CheckPackageInfo_TimeOutGenerator
    {
        double fTime = 0;
        double fInternalTime = -1;
        public void SetInternalTime(double fInternalTime)
        {
            this.Reset();
            this.fInternalTime = fInternalTime;
        }

        public void Reset()
        {
            this.fInternalTime = -1;
            this.fTime = 0.0;
        }

        public bool orSetInternalTime()
        {
            return fInternalTime >= 0.0;
        }

        public bool orTimeOut(double fElapsed)
        {
            this.fTime += fElapsed;
            if (this.fTime >= fInternalTime)
            {
                this.Reset();
                return true;
            }

            return false;
        }
    }

    internal interface ReSendPackageMgrInterface
    {
        void ReceiveOrderIdRequestPackage(ushort nRequestOrderId);
        void Update(double elapsed);
        void Reset();
    }

}
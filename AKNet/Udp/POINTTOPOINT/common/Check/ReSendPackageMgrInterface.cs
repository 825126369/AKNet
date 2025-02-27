/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp.POINTTOPOINT.Common
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
        void Add(NetUdpFixedSizePackage mPackage);
        void ReceiveOrderIdRequestPackage(ushort nRequestOrderId);
        void ReceiveOrderIdSurePackage(ushort nSureOrderId);
        void Update(double elapsed);
        void Reset();
    }

}
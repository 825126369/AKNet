/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal class CheckPackageInfo_TimeOutGenerator
    {
        double fTime = 0;
        double fInternalTime = 0;
        public void SetInternalTime(double fInternalTime)
        {
            this.fInternalTime = fInternalTime;
            this.Reset();
        }

        public void Reset()
        {
            this.fTime = 0.0;
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

    internal interface CheckPackageMgrInterface
    {
        void Add(NetUdpFixedSizePackage mPackage);
        void ReceiveOrderIdRequestPackage(ushort nRequestOrderId);
        void ReceiveOrderIdSurePackage(ushort nSureOrderId);
        void Update(double elapsed);
        void Reset();
    }

}
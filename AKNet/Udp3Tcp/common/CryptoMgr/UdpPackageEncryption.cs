/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp3Tcp.Common
{
    /// <summary>
    /// 把数据拿出来
    /// </summary>
    internal static class UdpPackageEncryption
    {
        private static readonly byte[] mCheck = new byte[4] { (byte)'$', (byte)'$', (byte)'$', (byte)'$' };
        private static readonly byte[] mCacheSendHeadBuffer = new byte[Config.nUdpPackageFixedHeadSize];

        public static byte[] EncodeHead(NetUdpSendFixedSizePackage mPackage)
        {
            byte nPackageId = mPackage.nPackageId;
            uint nOrderId = mPackage.nOrderId;
            uint nRequestOrderId = mPackage.nRequestOrderId;

            Array.Copy(mCheck, 0, mCacheSendHeadBuffer, 0, 4);

            byte[] byCom = BitConverter.GetBytes(nOrderId);
            Array.Copy(byCom, 0, mCacheSendHeadBuffer, 4, byCom.Length);

            byCom = BitConverter.GetBytes(nRequestOrderId);
            Array.Copy(byCom, 0, mCacheSendHeadBuffer, 8, byCom.Length);

            mCacheSendHeadBuffer[12] = nPackageId;
            return mCacheSendHeadBuffer;
        }

	}
}

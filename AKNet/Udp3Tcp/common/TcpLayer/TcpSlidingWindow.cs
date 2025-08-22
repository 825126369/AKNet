/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp3Tcp.Common
{
    internal class TcpSlidingWindow:AkCircularManyBuffer
    {
        public uint nBeginOrderId; //待确认的OrderId

        public TcpSlidingWindow(int nInitBlockCount = 1, int nMaxBlockCount = -1, int nBlockSize = Config.nUdpPackageFixedBodySize):
            base(nInitBlockCount, nMaxBlockCount, nBlockSize)
        {
            nBeginOrderId = Config.nUdpMinOrderId;
        }

        public void DoWindowForward(uint nRequestOrderId)
        {
            int nClearLength = GetWindowOffset(nRequestOrderId);
            ClearBuffer(nClearLength);
            nBeginOrderId = nRequestOrderId;
        }

        public int GetWindowOffset(uint nOrderId)
        {
            return OrderIdHelper.GetOrderIdLength(nBeginOrderId, nOrderId);
        }

        public void WindowReset()
        {
            base.Reset();
            nBeginOrderId = Config.nUdpMinOrderId;
        }
    }
}

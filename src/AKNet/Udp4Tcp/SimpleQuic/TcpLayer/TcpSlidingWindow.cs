/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:17
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp4Tcp.Common
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

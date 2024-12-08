using AKNet.Common;

namespace AKNet.Udp3Tcp.Common
{
    internal class TcpSlidingWindow:AkCircularBuffer
    {
        public uint nBeginOrderId;

        public TcpSlidingWindow()
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
            base.reset();
            nBeginOrderId = Config.nUdpMinOrderId;
        }
    }
}

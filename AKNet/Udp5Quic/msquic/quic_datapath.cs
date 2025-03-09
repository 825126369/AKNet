namespace AKNet.Udp5Quic.Common
{
    internal delegate void CXPLAT_DATAPATH_RECEIVE_CALLBACK (CXPLAT_SOCKET Socket, void* Context,CXPLAT_RECV_DATA* RecvDataChain);

    internal class CXPLAT_UDP_DATAPATH_CALLBACKS
    {
        CXPLAT_DATAPATH_RECEIVE_CALLBACK_HANDLER Receive;
        CXPLAT_DATAPATH_UNREACHABLE_CALLBACK_HANDLER Unreachable;
    }
}

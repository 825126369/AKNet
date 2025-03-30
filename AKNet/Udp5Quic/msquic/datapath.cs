using AKNet.Common;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    public class CXPLAT_SOCKET_POOL
    {
        public readonly object Lock = new object();
        public CXPLAT_HASHTABLE Sockets;
    }

    internal class CXPLAT_ROUTE_RESOLUTION_WORKER
    {
        public bool Enabled;
        public CXPLAT_EVENT Ready;
        public Thread Thread;
        public CXPLAT_POOL OperationPool;
        public readonly object Lock = new object();
        public CXPLAT_LIST_ENTRY Operations;
    }

    internal class CXPLAT_DATAPATH_RAW
    {
        public CXPLAT_DATAPATH ParentDataPath;
        public CXPLAT_WORKER_POOL WorkerPool;
        public CXPLAT_SOCKET_POOL SocketPool;
        public CXPLAT_ROUTE_RESOLUTION_WORKER RouteResolutionWorker;

        public CXPLAT_LIST_ENTRY Interfaces;
        public bool UseTcp;
    }

    internal static partial class MSQuicFunc
    {
        static ulong CxPlatDataPathInitialize(uint ClientRecvContextLength, CXPLAT_UDP_DATAPATH_CALLBACKS UdpCallbacks, 
            CXPLAT_TCP_DATAPATH_CALLBACKS TcpCallbacks, CXPLAT_WORKER_POOL WorkerPool,
            QUIC_EXECUTION_CONFIG Config, CXPLAT_DATAPATH NewDataPath)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            if (NewDataPath == null)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            Status = DataPathInitialize(
                    ClientRecvContextLength,
                    UdpCallbacks,
                    TcpCallbacks,
                    WorkerPool,
                    Config,
                    NewDataPath);

            if (QUIC_FAILED(Status))
            {
                QuicTraceLogVerbose(DatapathInitFail, "[  dp] Failed to initialize datapath, status:%d", Status);
                goto Error;
            }

            if (Config && Config->Flags & QUIC_EXECUTION_CONFIG_FLAG_XDP)
            {
                Status =
                    RawDataPathInitialize(
                        ClientRecvContextLength,
                        Config,
                        (*NewDataPath),
                        WorkerPool,
                        &((*NewDataPath)->RawDataPath));
                if (QUIC_FAILED(Status))
                {
                    QuicTraceLogVerbose(
                        RawDatapathInitFail,
                        "[ raw] Failed to initialize raw datapath, status:%d", Status);
                    (*NewDataPath)->RawDataPath = NULL;
                    CxPlatDataPathUninitialize(*NewDataPath);
                    *NewDataPath = NULL;
                }
            }

        Error:

            return Status;
        }

        static void QuicCopyRouteInfo(CXPLAT_ROUTE DstRoute, CXPLAT_ROUTE SrcRoute)
        {
            if (SrcRoute.DatapathType == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_RAW)
            {
                DstRoute.CopyFrom(SrcRoute);
                CxPlatUpdateRoute(DstRoute, SrcRoute);
            }
            else if (SrcRoute.DatapathType ==  CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_NORMAL)
            {
                DstRoute = SrcRoute;
            }
            else
            {
                NetLog.Assert(false);
            }
        }

        static void CxPlatUpdateRoute(CXPLAT_ROUTE DstRoute, CXPLAT_ROUTE SrcRoute)
        {
            if (SrcRoute.DatapathType == CXPLAT_DATAPATH_TYPE.CXPLAT_DATAPATH_TYPE_RAW)
            {
                RawUpdateRoute(DstRoute, SrcRoute);
            }

            if (DstRoute.DatapathType != SrcRoute.DatapathType ||
                (DstRoute.State == CXPLAT_ROUTE_STATE.RouteResolved &&
                 DstRoute.Queue != SrcRoute.Queue))
            {
                DstRoute.Queue = SrcRoute.Queue;
                DstRoute.DatapathType = SrcRoute.DatapathType;
            }
        }

        static void CxPlatRecvDataReturn(CXPLAT_RECV_DATA RecvDataChain)
        {
            if (RecvDataChain == null)
            {
                return;
            }
            NetLog.Assert(
                RecvDataChain->DatapathType == CXPLAT_DATAPATH_TYPE_NORMAL ||
                RecvDataChain->DatapathType == CXPLAT_DATAPATH_TYPE_RAW);
            RecvDataChain->DatapathType == CXPLAT_DATAPATH_TYPE_NORMAL ?
                RecvDataReturn(RecvDataChain) : RawRecvDataReturn(RecvDataChain);
        }
    }
}

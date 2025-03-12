namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        long CxPlatDataPathInitialize(uint ClientRecvContextLength, CXPLAT_UDP_DATAPATH_CALLBACKS UdpCallbacks,CXPLAT_TCP_DATAPATH_CALLBACKS TcpCallbacks,
                CXPLAT_WORKER_POOL WorkerPool,
                QUIC_EXECUTION_CONFIG Config,
                CXPLAT_DATAPATH NewDataPath
                )
            {
                long Status = QUIC_STATUS_SUCCESS;
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
    }
}

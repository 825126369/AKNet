namespace AKNet.QuicNet.Common
{
    public enum StreamState
    {
        Recv,
        SizeKnown,
        DataRecvd,
        DataRead,
        ResetRecvd
    }
}

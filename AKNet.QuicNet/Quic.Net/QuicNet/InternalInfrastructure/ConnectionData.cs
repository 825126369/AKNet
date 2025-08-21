namespace AKNet.QuicNet.Common
{
    internal class ConnectionData
    {
        public PacketWireTransfer PWT { get; set; }
        public GranularInteger ConnectionId { get; set; }
        public GranularInteger PeerConnectionId { get; set; }

        public ConnectionData(PacketWireTransfer pwt, GranularInteger connectionId, GranularInteger peerConnnectionId)
        {
            PWT = pwt;
            ConnectionId = connectionId;
            PeerConnectionId = peerConnnectionId;
        }
    }
}

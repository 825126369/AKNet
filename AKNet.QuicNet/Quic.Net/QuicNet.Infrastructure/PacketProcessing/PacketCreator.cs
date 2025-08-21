namespace AKNet.QuicNet.Common
{
    public class PacketCreator
    {
        private readonly NumberSpace _ns;
        private readonly GranularInteger _connectionId;
        private readonly GranularInteger _peerConnectionId;

        public PacketCreator(GranularInteger connectionId, GranularInteger peerConnectionId)
        {
            _ns = new NumberSpace();

            _connectionId = connectionId;
            _peerConnectionId = peerConnectionId;
        }

        public ShortHeaderPacket CreateConnectionClosePacket(ErrorCode code, byte frameType, string reason)
        {
            ShortHeaderPacket packet = new ShortHeaderPacket(_peerConnectionId.Size);
            packet.PacketNumber = _ns.Get();
            packet.DestinationConnectionId = (byte)_peerConnectionId;
            packet.AttachFrame(new ConnectionCloseFrame(code, frameType, reason));

            return packet;
        }

        public ShortHeaderPacket CreateDataPacket(UInt64 streamId, byte[] data, UInt64 offset, bool eos)
        {
            ShortHeaderPacket packet = new ShortHeaderPacket(_peerConnectionId.Size);
            packet.PacketNumber = _ns.Get();
            packet.DestinationConnectionId = (byte)_peerConnectionId;
            packet.AttachFrame(new StreamFrame(streamId, data, offset, eos));

            return packet;
        }

        public InitialPacket CreateServerBusyPacket()
        {
            return new InitialPacket(0, 0);
        }

        public ShortHeaderPacket CreateShortHeaderPacket()
        {
            return new ShortHeaderPacket(0);
        }
    }
}

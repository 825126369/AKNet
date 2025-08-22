namespace AKNet.QuicNet.Common
{
    public class InitialPacketCreator
    {
        public InitialPacket CreateInitialPacket(GranularInteger sourceConnectionId, GranularInteger destinationConnectionId)
        {
            InitialPacket packet = new InitialPacket(destinationConnectionId, sourceConnectionId);
            packet.PacketNumber = 0;
            packet.SourceConnectionId = sourceConnectionId;
            packet.DestinationConnectionId = destinationConnectionId;
            packet.Version = QuicVersion.CurrentVersion;

            int length = packet.Encode().Length;
            int padding = QuicSettings.PMTU - length;

            for (int i = 0; i < padding; i++)
                packet.AttachFrame(new PaddingFrame());

            return packet;
        }

        public VersionNegotiationPacket CreateVersionNegotiationPacket()
        {
            return new VersionNegotiationPacket();
        }
    }
}

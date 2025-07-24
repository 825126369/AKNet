//namespace AKNet.Platform.Socket
//{
//    public struct IPPacketInformation : IEquatable<IPPacketInformation>
//    {
//        private readonly IPAddress _address;
//        private readonly int _networkInterface;

//        internal IPPacketInformation(IPAddress address, int networkInterface)
//        {
//            _address = address;
//            _networkInterface = networkInterface;
//        }

//        public IPAddress Address => _address;
//        public int Interface => _networkInterface;

//        public static bool operator ==(IPPacketInformation packetInformation1, IPPacketInformation packetInformation2) =>
//            packetInformation1.Equals(packetInformation2);

//        public static bool operator !=(IPPacketInformation packetInformation1, IPPacketInformation packetInformation2) =>
//            !packetInformation1.Equals(packetInformation2);

//        public override bool Equals(object? comparand) =>
//            comparand is IPPacketInformation other && Equals(other);
        
//        public bool Equals(IPPacketInformation other) =>
//            _networkInterface == other._networkInterface &&
//            (_address is null ? other._address is null : _address.Equals(other._address));

//        public override int GetHashCode() =>
//            unchecked(_networkInterface.GetHashCode() * (int)0xA5555529) + (_address?.GetHashCode() ?? 0);
//    }
//}

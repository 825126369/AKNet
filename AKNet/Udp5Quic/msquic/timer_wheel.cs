namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_TIMER_WHEEL
    {
        //
        // The expiration time (in us) for the next timer in the timer wheel.
        //
        public ulong NextExpirationTime;

        //
        // Total number of connections in the timer wheel.
        //
        public ulong ConnectionCount;

        //
        // The connection with the timer that expires next.
        //
        public QUIC_CONNECTION NextConnection;

        //
        // The number of slots in the Slots array.
        //
        public uint SlotCount;

        //
        // An array of slots in the timer wheel.
        //
        public CXPLAT_LIST_ENTRY Slots;
    }
}

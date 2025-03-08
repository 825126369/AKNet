using System.Collections.Generic;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_LISTENER
    {
        public bool WildCard;
        public bool AppClosed;
        public bool Stopped;
        public bool NeedsCleanup;
        // CXPLAT_THREAD_ID StopCompleteThreadID;
        //CXPLAT_LIST_ENTRY Link;
        //QUIC_REGISTRATION* Registration;
        //CXPLAT_LIST_ENTRY RegistrationLink;
        //CXPLAT_REF_COUNT RefCount;
        //CXPLAT_EVENT StopEvent;
        public string LocalAddress;
        //QUIC_BINDING* Binding;
        //QUIC_LISTENER_CALLBACK_HANDLER ClientCallbackHandler;

        public ulong TotalAcceptedConnections;
        public ulong TotalRejectedConnections;
        public readonly List<byte> AlpnList = new List<byte>();
        public byte[] CibirId = new byte[2 + MSQuicFunc.QUIC_MAX_CIBIR_LENGTH];
    }
}

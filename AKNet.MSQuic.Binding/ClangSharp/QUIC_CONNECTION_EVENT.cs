using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    public unsafe partial struct QUIC_CONNECTION_EVENT
    {
        public QUIC_CONNECTION_EVENT_TYPE Type;

        [NativeTypeName("__AnonymousRecord_msquic_L1182_C5")]
        public _Anonymous_e__Union Anonymous;

        public ref _Anonymous_e__Union._CONNECTED_e__Struct CONNECTED
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->CONNECTED;
                }
            }
        }

        public ref _Anonymous_e__Union._SHUTDOWN_INITIATED_BY_TRANSPORT_e__Struct SHUTDOWN_INITIATED_BY_TRANSPORT
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->SHUTDOWN_INITIATED_BY_TRANSPORT;
                }
            }
        }

        public ref _Anonymous_e__Union._SHUTDOWN_INITIATED_BY_PEER_e__Struct SHUTDOWN_INITIATED_BY_PEER
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->SHUTDOWN_INITIATED_BY_PEER;
                }
            }
        }

        public ref _Anonymous_e__Union._SHUTDOWN_COMPLETE_e__Struct SHUTDOWN_COMPLETE
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->SHUTDOWN_COMPLETE;
                }
            }
        }

        public ref _Anonymous_e__Union._LOCAL_ADDRESS_CHANGED_e__Struct LOCAL_ADDRESS_CHANGED
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->LOCAL_ADDRESS_CHANGED;
                }
            }
        }

        public ref _Anonymous_e__Union._PEER_ADDRESS_CHANGED_e__Struct PEER_ADDRESS_CHANGED
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->PEER_ADDRESS_CHANGED;
                }
            }
        }

        public ref _Anonymous_e__Union._PEER_STREAM_STARTED_e__Struct PEER_STREAM_STARTED
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->PEER_STREAM_STARTED;
                }
            }
        }

        public ref _Anonymous_e__Union._STREAMS_AVAILABLE_e__Struct STREAMS_AVAILABLE
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->STREAMS_AVAILABLE;
                }
            }
        }

        public ref _Anonymous_e__Union._PEER_NEEDS_STREAMS_e__Struct PEER_NEEDS_STREAMS
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->PEER_NEEDS_STREAMS;
                }
            }
        }

        public ref _Anonymous_e__Union._IDEAL_PROCESSOR_CHANGED_e__Struct IDEAL_PROCESSOR_CHANGED
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->IDEAL_PROCESSOR_CHANGED;
                }
            }
        }

        public ref _Anonymous_e__Union._DATAGRAM_STATE_CHANGED_e__Struct DATAGRAM_STATE_CHANGED
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->DATAGRAM_STATE_CHANGED;
                }
            }
        }

        public ref _Anonymous_e__Union._DATAGRAM_RECEIVED_e__Struct DATAGRAM_RECEIVED
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->DATAGRAM_RECEIVED;
                }
            }
        }

        public ref _Anonymous_e__Union._DATAGRAM_SEND_STATE_CHANGED_e__Struct DATAGRAM_SEND_STATE_CHANGED
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->DATAGRAM_SEND_STATE_CHANGED;
                }
            }
        }

        public ref _Anonymous_e__Union._RESUMED_e__Struct RESUMED
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->RESUMED;
                }
            }
        }

        public ref _Anonymous_e__Union._RESUMPTION_TICKET_RECEIVED_e__Struct RESUMPTION_TICKET_RECEIVED
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->RESUMPTION_TICKET_RECEIVED;
                }
            }
        }

        public ref _Anonymous_e__Union._PEER_CERTIFICATE_RECEIVED_e__Struct PEER_CERTIFICATE_RECEIVED
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->PEER_CERTIFICATE_RECEIVED;
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1183_C9")]
            public _CONNECTED_e__Struct CONNECTED;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1190_C9")]
            public _SHUTDOWN_INITIATED_BY_TRANSPORT_e__Struct SHUTDOWN_INITIATED_BY_TRANSPORT;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1194_C9")]
            public _SHUTDOWN_INITIATED_BY_PEER_e__Struct SHUTDOWN_INITIATED_BY_PEER;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1197_C9")]
            public _SHUTDOWN_COMPLETE_e__Struct SHUTDOWN_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1202_C9")]
            public _LOCAL_ADDRESS_CHANGED_e__Struct LOCAL_ADDRESS_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1205_C9")]
            public _PEER_ADDRESS_CHANGED_e__Struct PEER_ADDRESS_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1208_C9")]
            public _PEER_STREAM_STARTED_e__Struct PEER_STREAM_STARTED;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1212_C9")]
            public _STREAMS_AVAILABLE_e__Struct STREAMS_AVAILABLE;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1216_C9")]
            public _PEER_NEEDS_STREAMS_e__Struct PEER_NEEDS_STREAMS;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1219_C9")]
            public _IDEAL_PROCESSOR_CHANGED_e__Struct IDEAL_PROCESSOR_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1223_C9")]
            public _DATAGRAM_STATE_CHANGED_e__Struct DATAGRAM_STATE_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1227_C9")]
            public _DATAGRAM_RECEIVED_e__Struct DATAGRAM_RECEIVED;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1231_C9")]
            public _DATAGRAM_SEND_STATE_CHANGED_e__Struct DATAGRAM_SEND_STATE_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1235_C9")]
            public _RESUMED_e__Struct RESUMED;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1239_C9")]
            public _RESUMPTION_TICKET_RECEIVED_e__Struct RESUMPTION_TICKET_RECEIVED;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1245_C9")]
            public _PEER_CERTIFICATE_RECEIVED_e__Struct PEER_CERTIFICATE_RECEIVED;

            public unsafe partial struct _CONNECTED_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                public byte SessionResumed;

                [NativeTypeName("uint8_t")]
                public byte NegotiatedAlpnLength;

                [NativeTypeName("const uint8_t *")]
                public byte* NegotiatedAlpn;
            }

            public partial struct _SHUTDOWN_INITIATED_BY_TRANSPORT_e__Struct
            {
                [NativeTypeName("HRESULT")]
                public int Status;

                [NativeTypeName("QUIC_UINT62")]
                public ulong ErrorCode;
            }

            public partial struct _SHUTDOWN_INITIATED_BY_PEER_e__Struct
            {
                [NativeTypeName("QUIC_UINT62")]
                public ulong ErrorCode;
            }

            public partial struct _SHUTDOWN_COMPLETE_e__Struct
            {
                public byte _bitfield;

                [NativeTypeName("BOOLEAN : 1")]
                public byte HandshakeCompleted
                {
                    get
                    {
                        return (byte)(_bitfield & 0x1u);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~0x1u) | (value & 0x1u));
                    }
                }

                [NativeTypeName("BOOLEAN : 1")]
                public byte PeerAcknowledgedShutdown
                {
                    get
                    {
                        return (byte)((_bitfield >> 1) & 0x1u);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1));
                    }
                }

                [NativeTypeName("BOOLEAN : 1")]
                public byte AppCloseInProgress
                {
                    get
                    {
                        return (byte)((_bitfield >> 2) & 0x1u);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2));
                    }
                }
            }

            public unsafe partial struct _LOCAL_ADDRESS_CHANGED_e__Struct
            {
                [NativeTypeName("const QUIC_ADDR *")]
                public _SOCKADDR_INET* Address;
            }

            public unsafe partial struct _PEER_ADDRESS_CHANGED_e__Struct
            {
                [NativeTypeName("const QUIC_ADDR *")]
                public _SOCKADDR_INET* Address;
            }

            public unsafe partial struct _PEER_STREAM_STARTED_e__Struct
            {
                [NativeTypeName("HQUIC")]
                public QUIC_HANDLE* Stream;

                public QUIC_STREAM_OPEN_FLAGS Flags;
            }

            public partial struct _STREAMS_AVAILABLE_e__Struct
            {
                [NativeTypeName("uint16_t")]
                public ushort BidirectionalCount;

                [NativeTypeName("uint16_t")]
                public ushort UnidirectionalCount;
            }

            public partial struct _PEER_NEEDS_STREAMS_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                public byte Bidirectional;
            }

            public partial struct _IDEAL_PROCESSOR_CHANGED_e__Struct
            {
                [NativeTypeName("uint16_t")]
                public ushort IdealProcessor;

                [NativeTypeName("uint16_t")]
                public ushort PartitionIndex;
            }

            public partial struct _DATAGRAM_STATE_CHANGED_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                public byte SendEnabled;

                [NativeTypeName("uint16_t")]
                public ushort MaxSendLength;
            }

            public unsafe partial struct _DATAGRAM_RECEIVED_e__Struct
            {
                [NativeTypeName("const QUIC_BUFFER *")]
                public QUIC_BUFFER* Buffer;

                public QUIC_RECEIVE_FLAGS Flags;
            }

            public unsafe partial struct _DATAGRAM_SEND_STATE_CHANGED_e__Struct
            {
                public void* ClientContext;

                public QUIC_DATAGRAM_SEND_STATE State;
            }

            public unsafe partial struct _RESUMED_e__Struct
            {
                [NativeTypeName("uint16_t")]
                public ushort ResumptionStateLength;

                [NativeTypeName("const uint8_t *")]
                public byte* ResumptionState;
            }

            public unsafe partial struct _RESUMPTION_TICKET_RECEIVED_e__Struct
            {
                [NativeTypeName("uint32_t")]
                public uint ResumptionTicketLength;

                [NativeTypeName("const uint8_t *")]
                public byte* ResumptionTicket;
            }

            public unsafe partial struct _PEER_CERTIFICATE_RECEIVED_e__Struct
            {
                [NativeTypeName("QUIC_CERTIFICATE *")]
                public void* Certificate;

                [NativeTypeName("uint32_t")]
                public uint DeferredErrorFlags;

                [NativeTypeName("HRESULT")]
                public int DeferredStatus;

                [NativeTypeName("QUIC_CERTIFICATE_CHAIN *")]
                public void* Chain;
            }
        }
    }
}

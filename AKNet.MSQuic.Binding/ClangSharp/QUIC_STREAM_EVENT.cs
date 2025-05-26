using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    public unsafe partial struct QUIC_STREAM_EVENT
    {
        public QUIC_STREAM_EVENT_TYPE Type;

        [NativeTypeName("__AnonymousRecord_msquic_L1414_C5")]
        public _Anonymous_e__Union Anonymous;

        public ref _Anonymous_e__Union._START_COMPLETE_e__Struct START_COMPLETE
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->START_COMPLETE;
                }
            }
        }

        public ref _Anonymous_e__Union._RECEIVE_e__Struct RECEIVE
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->RECEIVE;
                }
            }
        }

        public ref _Anonymous_e__Union._SEND_COMPLETE_e__Struct SEND_COMPLETE
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->SEND_COMPLETE;
                }
            }
        }

        public ref _Anonymous_e__Union._PEER_SEND_ABORTED_e__Struct PEER_SEND_ABORTED
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->PEER_SEND_ABORTED;
                }
            }
        }

        public ref _Anonymous_e__Union._PEER_RECEIVE_ABORTED_e__Struct PEER_RECEIVE_ABORTED
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->PEER_RECEIVE_ABORTED;
                }
            }
        }

        public ref _Anonymous_e__Union._SEND_SHUTDOWN_COMPLETE_e__Struct SEND_SHUTDOWN_COMPLETE
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->SEND_SHUTDOWN_COMPLETE;
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

        public ref _Anonymous_e__Union._IDEAL_SEND_BUFFER_SIZE_e__Struct IDEAL_SEND_BUFFER_SIZE
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->IDEAL_SEND_BUFFER_SIZE;
                }
            }
        }

        public ref _Anonymous_e__Union._CANCEL_ON_LOSS_e__Struct CANCEL_ON_LOSS
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->CANCEL_ON_LOSS;
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1415_C9")]
            public _START_COMPLETE_e__Struct START_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1421_C9")]
            public _RECEIVE_e__Struct RECEIVE;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1430_C9")]
            public _SEND_COMPLETE_e__Struct SEND_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1434_C9")]
            public _PEER_SEND_ABORTED_e__Struct PEER_SEND_ABORTED;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1437_C9")]
            public _PEER_RECEIVE_ABORTED_e__Struct PEER_RECEIVE_ABORTED;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1440_C9")]
            public _SEND_SHUTDOWN_COMPLETE_e__Struct SEND_SHUTDOWN_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1443_C9")]
            public _SHUTDOWN_COMPLETE_e__Struct SHUTDOWN_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1452_C9")]
            public _IDEAL_SEND_BUFFER_SIZE_e__Struct IDEAL_SEND_BUFFER_SIZE;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L1455_C9")]
            public _CANCEL_ON_LOSS_e__Struct CANCEL_ON_LOSS;

            public partial struct _START_COMPLETE_e__Struct
            {
                [NativeTypeName("HRESULT")]
                public int Status;

                [NativeTypeName("QUIC_UINT62")]
                public ulong ID;

                public byte _bitfield;

                [NativeTypeName("BOOLEAN : 1")]
                public byte PeerAccepted
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

                [NativeTypeName("BOOLEAN : 7")]
                public byte RESERVED
                {
                    get
                    {
                        return (byte)((_bitfield >> 1) & 0x7Fu);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~(0x7Fu << 1)) | ((value & 0x7Fu) << 1));
                    }
                }
            }

            public unsafe partial struct _RECEIVE_e__Struct
            {
                [NativeTypeName("uint64_t")]
                public ulong AbsoluteOffset;

                [NativeTypeName("uint64_t")]
                public ulong TotalBufferLength;

                [NativeTypeName("const QUIC_BUFFER *")]
                public QUIC_BUFFER* Buffers;

                [NativeTypeName("uint32_t")]
                public uint BufferCount;

                public QUIC_RECEIVE_FLAGS Flags;
            }

            public unsafe partial struct _SEND_COMPLETE_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                public byte Canceled;

                public void* ClientContext;
            }

            public partial struct _PEER_SEND_ABORTED_e__Struct
            {
                [NativeTypeName("QUIC_UINT62")]
                public ulong ErrorCode;
            }

            public partial struct _PEER_RECEIVE_ABORTED_e__Struct
            {
                [NativeTypeName("QUIC_UINT62")]
                public ulong ErrorCode;
            }

            public partial struct _SEND_SHUTDOWN_COMPLETE_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                public byte Graceful;
            }

            public partial struct _SHUTDOWN_COMPLETE_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                public byte ConnectionShutdown;

                public byte _bitfield;

                [NativeTypeName("BOOLEAN : 1")]
                public byte AppCloseInProgress
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
                public byte ConnectionShutdownByApp
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
                public byte ConnectionClosedRemotely
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

                [NativeTypeName("BOOLEAN : 5")]
                public byte RESERVED
                {
                    get
                    {
                        return (byte)((_bitfield >> 3) & 0x1Fu);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~(0x1Fu << 3)) | ((value & 0x1Fu) << 3));
                    }
                }

                [NativeTypeName("QUIC_UINT62")]
                public ulong ConnectionErrorCode;

                [NativeTypeName("HRESULT")]
                public int ConnectionCloseStatus;
            }

            public partial struct _IDEAL_SEND_BUFFER_SIZE_e__Struct
            {
                [NativeTypeName("uint64_t")]
                public ulong ByteCount;
            }

            public partial struct _CANCEL_ON_LOSS_e__Struct
            {
                [NativeTypeName("QUIC_UINT62")]
                public ulong ErrorCode;
            }
        }
    }
}

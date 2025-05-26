using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    public unsafe partial struct QUIC_SETTINGS
    {
        [NativeTypeName("__AnonymousRecord_msquic_L643_C5")]
        public _Anonymous1_e__Union Anonymous1;

        [NativeTypeName("uint64_t")]
        public ulong MaxBytesPerKey;

        [NativeTypeName("uint64_t")]
        public ulong HandshakeIdleTimeoutMs;

        [NativeTypeName("uint64_t")]
        public ulong IdleTimeoutMs;

        [NativeTypeName("uint64_t")]
        public ulong MtuDiscoverySearchCompleteTimeoutUs;

        [NativeTypeName("uint32_t")]
        public uint TlsClientMaxSendBuffer;

        [NativeTypeName("uint32_t")]
        public uint TlsServerMaxSendBuffer;

        [NativeTypeName("uint32_t")]
        public uint StreamRecvWindowDefault;

        [NativeTypeName("uint32_t")]
        public uint StreamRecvBufferDefault;

        [NativeTypeName("uint32_t")]
        public uint ConnFlowControlWindow;

        [NativeTypeName("uint32_t")]
        public uint MaxWorkerQueueDelayUs;

        [NativeTypeName("uint32_t")]
        public uint MaxStatelessOperations;

        [NativeTypeName("uint32_t")]
        public uint InitialWindowPackets;

        [NativeTypeName("uint32_t")]
        public uint SendIdleTimeoutMs;

        [NativeTypeName("uint32_t")]
        public uint InitialRttMs;

        [NativeTypeName("uint32_t")]
        public uint MaxAckDelayMs;

        [NativeTypeName("uint32_t")]
        public uint DisconnectTimeoutMs;

        [NativeTypeName("uint32_t")]
        public uint KeepAliveIntervalMs;

        [NativeTypeName("uint16_t")]
        public ushort CongestionControlAlgorithm;

        [NativeTypeName("uint16_t")]
        public ushort PeerBidiStreamCount;

        [NativeTypeName("uint16_t")]
        public ushort PeerUnidiStreamCount;

        [NativeTypeName("uint16_t")]
        public ushort MaxBindingStatelessOperations;

        [NativeTypeName("uint16_t")]
        public ushort StatelessOperationExpirationMs;

        [NativeTypeName("uint16_t")]
        public ushort MinimumMtu;

        [NativeTypeName("uint16_t")]
        public ushort MaximumMtu;

        public byte _bitfield;

        [NativeTypeName("uint8_t : 1")]
        public byte SendBufferingEnabled
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

        [NativeTypeName("uint8_t : 1")]
        public byte PacingEnabled
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

        [NativeTypeName("uint8_t : 1")]
        public byte MigrationEnabled
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

        [NativeTypeName("uint8_t : 1")]
        public byte DatagramReceiveEnabled
        {
            get
            {
                return (byte)((_bitfield >> 3) & 0x1u);
            }

            set
            {
                _bitfield = (byte)((_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3));
            }
        }

        [NativeTypeName("uint8_t : 2")]
        public byte ServerResumptionLevel
        {
            get
            {
                return (byte)((_bitfield >> 4) & 0x3u);
            }

            set
            {
                _bitfield = (byte)((_bitfield & ~(0x3u << 4)) | ((value & 0x3u) << 4));
            }
        }

        [NativeTypeName("uint8_t : 1")]
        public byte GreaseQuicBitEnabled
        {
            get
            {
                return (byte)((_bitfield >> 6) & 0x1u);
            }

            set
            {
                _bitfield = (byte)((_bitfield & ~(0x1u << 6)) | ((value & 0x1u) << 6));
            }
        }

        [NativeTypeName("uint8_t : 1")]
        public byte EcnEnabled
        {
            get
            {
                return (byte)((_bitfield >> 7) & 0x1u);
            }

            set
            {
                _bitfield = (byte)((_bitfield & ~(0x1u << 7)) | ((value & 0x1u) << 7));
            }
        }

        [NativeTypeName("uint8_t")]
        public byte MaxOperationsPerDrain;

        [NativeTypeName("uint8_t")]
        public byte MtuDiscoveryMissingProbeCount;

        [NativeTypeName("uint32_t")]
        public uint DestCidUpdateIdleTimeoutMs;

        [NativeTypeName("__AnonymousRecord_msquic_L731_C5")]
        public _Anonymous2_e__Union Anonymous2;

        [NativeTypeName("uint32_t")]
        public uint StreamRecvWindowBidiLocalDefault;

        [NativeTypeName("uint32_t")]
        public uint StreamRecvWindowBidiRemoteDefault;

        [NativeTypeName("uint32_t")]
        public uint StreamRecvWindowUnidiDefault;

        public ref ulong IsSetFlags
        {
            get
            {
                fixed (_Anonymous1_e__Union* pField = &Anonymous1)
                {
                    return ref pField->IsSetFlags;
                }
            }
        }

        public ref _Anonymous1_e__Union._IsSet_e__Struct IsSet
        {
            get
            {
                fixed (_Anonymous1_e__Union* pField = &Anonymous1)
                {
                    return ref pField->IsSet;
                }
            }
        }

        public ref ulong Flags
        {
            get
            {
                fixed (_Anonymous2_e__Union* pField = &Anonymous2)
                {
                    return ref pField->Flags;
                }
            }
        }

        public ulong HyStartEnabled
        {
            get
            {
                return Anonymous2.Anonymous_1.HyStartEnabled;
            }

            set
            {
                Anonymous2.Anonymous_1.HyStartEnabled = value;
            }
        }

        public ulong ReservedFlags
        {
            get
            {
                return Anonymous2.Anonymous_1.ReservedFlags;
            }

            set
            {
                Anonymous2.Anonymous_1.ReservedFlags = value;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public unsafe partial struct _Anonymous1_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("uint64_t")]
            public ulong IsSetFlags;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L645_C9")]
            public _IsSet_e__Struct IsSet;

            public partial struct _IsSet_e__Struct
            {
                public ulong _bitfield;

                [NativeTypeName("uint64_t : 1")]
                public ulong MaxBytesPerKey
                {
                    get
                    {
                        return _bitfield & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~0x1UL) | (value & 0x1UL);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong HandshakeIdleTimeoutMs
                {
                    get
                    {
                        return (_bitfield >> 1) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 1)) | ((value & 0x1UL) << 1);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong IdleTimeoutMs
                {
                    get
                    {
                        return (_bitfield >> 2) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 2)) | ((value & 0x1UL) << 2);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MtuDiscoverySearchCompleteTimeoutUs
                {
                    get
                    {
                        return (_bitfield >> 3) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 3)) | ((value & 0x1UL) << 3);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong TlsClientMaxSendBuffer
                {
                    get
                    {
                        return (_bitfield >> 4) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 4)) | ((value & 0x1UL) << 4);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong TlsServerMaxSendBuffer
                {
                    get
                    {
                        return (_bitfield >> 5) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 5)) | ((value & 0x1UL) << 5);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong StreamRecvWindowDefault
                {
                    get
                    {
                        return (_bitfield >> 6) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 6)) | ((value & 0x1UL) << 6);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong StreamRecvBufferDefault
                {
                    get
                    {
                        return (_bitfield >> 7) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 7)) | ((value & 0x1UL) << 7);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong ConnFlowControlWindow
                {
                    get
                    {
                        return (_bitfield >> 8) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 8)) | ((value & 0x1UL) << 8);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MaxWorkerQueueDelayUs
                {
                    get
                    {
                        return (_bitfield >> 9) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 9)) | ((value & 0x1UL) << 9);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MaxStatelessOperations
                {
                    get
                    {
                        return (_bitfield >> 10) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 10)) | ((value & 0x1UL) << 10);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong InitialWindowPackets
                {
                    get
                    {
                        return (_bitfield >> 11) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 11)) | ((value & 0x1UL) << 11);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong SendIdleTimeoutMs
                {
                    get
                    {
                        return (_bitfield >> 12) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 12)) | ((value & 0x1UL) << 12);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong InitialRttMs
                {
                    get
                    {
                        return (_bitfield >> 13) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 13)) | ((value & 0x1UL) << 13);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MaxAckDelayMs
                {
                    get
                    {
                        return (_bitfield >> 14) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 14)) | ((value & 0x1UL) << 14);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong DisconnectTimeoutMs
                {
                    get
                    {
                        return (_bitfield >> 15) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 15)) | ((value & 0x1UL) << 15);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong KeepAliveIntervalMs
                {
                    get
                    {
                        return (_bitfield >> 16) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 16)) | ((value & 0x1UL) << 16);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong CongestionControlAlgorithm
                {
                    get
                    {
                        return (_bitfield >> 17) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 17)) | ((value & 0x1UL) << 17);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong PeerBidiStreamCount
                {
                    get
                    {
                        return (_bitfield >> 18) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 18)) | ((value & 0x1UL) << 18);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong PeerUnidiStreamCount
                {
                    get
                    {
                        return (_bitfield >> 19) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 19)) | ((value & 0x1UL) << 19);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MaxBindingStatelessOperations
                {
                    get
                    {
                        return (_bitfield >> 20) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 20)) | ((value & 0x1UL) << 20);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong StatelessOperationExpirationMs
                {
                    get
                    {
                        return (_bitfield >> 21) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 21)) | ((value & 0x1UL) << 21);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MinimumMtu
                {
                    get
                    {
                        return (_bitfield >> 22) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 22)) | ((value & 0x1UL) << 22);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MaximumMtu
                {
                    get
                    {
                        return (_bitfield >> 23) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 23)) | ((value & 0x1UL) << 23);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong SendBufferingEnabled
                {
                    get
                    {
                        return (_bitfield >> 24) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 24)) | ((value & 0x1UL) << 24);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong PacingEnabled
                {
                    get
                    {
                        return (_bitfield >> 25) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 25)) | ((value & 0x1UL) << 25);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MigrationEnabled
                {
                    get
                    {
                        return (_bitfield >> 26) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 26)) | ((value & 0x1UL) << 26);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong DatagramReceiveEnabled
                {
                    get
                    {
                        return (_bitfield >> 27) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 27)) | ((value & 0x1UL) << 27);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong ServerResumptionLevel
                {
                    get
                    {
                        return (_bitfield >> 28) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 28)) | ((value & 0x1UL) << 28);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MaxOperationsPerDrain
                {
                    get
                    {
                        return (_bitfield >> 29) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 29)) | ((value & 0x1UL) << 29);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MtuDiscoveryMissingProbeCount
                {
                    get
                    {
                        return (_bitfield >> 30) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 30)) | ((value & 0x1UL) << 30);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong DestCidUpdateIdleTimeoutMs
                {
                    get
                    {
                        return (_bitfield >> 31) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 31)) | ((value & 0x1UL) << 31);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong GreaseQuicBitEnabled
                {
                    get
                    {
                        return (_bitfield >> 32) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 32)) | ((value & 0x1UL) << 32);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong EcnEnabled
                {
                    get
                    {
                        return (_bitfield >> 33) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 33)) | ((value & 0x1UL) << 33);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong HyStartEnabled
                {
                    get
                    {
                        return (_bitfield >> 34) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 34)) | ((value & 0x1UL) << 34);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong StreamRecvWindowBidiLocalDefault
                {
                    get
                    {
                        return (_bitfield >> 35) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 35)) | ((value & 0x1UL) << 35);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong StreamRecvWindowBidiRemoteDefault
                {
                    get
                    {
                        return (_bitfield >> 36) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 36)) | ((value & 0x1UL) << 36);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong StreamRecvWindowUnidiDefault
                {
                    get
                    {
                        return (_bitfield >> 37) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 37)) | ((value & 0x1UL) << 37);
                    }
                }

                [NativeTypeName("uint64_t : 26")]
                public ulong RESERVED
                {
                    get
                    {
                        return (_bitfield >> 38) & 0x3FFFFFFUL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x3FFFFFFUL << 38)) | ((value & 0x3FFFFFFUL) << 38);
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public unsafe partial struct _Anonymous2_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("uint64_t")]
            public ulong Flags;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L733_C9")]
            public _Anonymous_1_e__Struct Anonymous_1;

            public partial struct _Anonymous_1_e__Struct
            {
                public ulong _bitfield;

                [NativeTypeName("uint64_t : 1")]
                public ulong HyStartEnabled
                {
                    get
                    {
                        return _bitfield & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~0x1UL) | (value & 0x1UL);
                    }
                }

                [NativeTypeName("uint64_t : 63")]
                public ulong ReservedFlags
                {
                    get
                    {
                        return (_bitfield >> 1) & 0x7FFFFFFFUL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x7FFFFFFFUL << 1)) | ((value & 0x7FFFFFFFUL) << 1);
                    }
                }
            }
        }
    }
}

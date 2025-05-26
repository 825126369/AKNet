using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    public unsafe partial struct QUIC_GLOBAL_SETTINGS
    {
        [NativeTypeName("__AnonymousRecord_msquic_L627_C5")]
        public _Anonymous_e__Union Anonymous;

        [NativeTypeName("uint16_t")]
        public ushort RetryMemoryLimit;

        [NativeTypeName("uint16_t")]
        public ushort LoadBalancingMode;

        [NativeTypeName("uint32_t")]
        public uint FixedServerID;

        public ref ulong IsSetFlags
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->IsSetFlags;
                }
            }
        }

        public ref _Anonymous_e__Union._IsSet_e__Struct IsSet
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->IsSet;
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("uint64_t")]
            public ulong IsSetFlags;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_msquic_L629_C9")]
            public _IsSet_e__Struct IsSet;

            public partial struct _IsSet_e__Struct
            {
                public ulong _bitfield;

                [NativeTypeName("uint64_t : 1")]
                public ulong RetryMemoryLimit
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
                public ulong LoadBalancingMode
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
                public ulong FixedServerID
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

                [NativeTypeName("uint64_t : 61")]
                public ulong RESERVED
                {
                    get
                    {
                        return (_bitfield >> 3) & 0x1FFFFFFFUL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1FFFFFFFUL << 3)) | ((value & 0x1FFFFFFFUL) << 3);
                    }
                }
            }
        }
    }
}

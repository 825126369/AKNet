using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

public unsafe partial struct QUIC_LISTENER_EVENT
{
    public QUIC_LISTENER_EVENT_TYPE Type;

    [NativeTypeName("__AnonymousRecord_msquic_L1079_C5")]
    public _Anonymous_e__Union Anonymous;

    public ref _Anonymous_e__Union._NEW_CONNECTION_e__Struct NEW_CONNECTION
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            fixed (_Anonymous_e__Union* pField = &Anonymous)
            {
                return ref pField->NEW_CONNECTION;
            }
        }
    }

    public ref _Anonymous_e__Union._STOP_COMPLETE_e__Struct STOP_COMPLETE
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            fixed (_Anonymous_e__Union* pField = &Anonymous)
            {
                return ref pField->STOP_COMPLETE;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        [NativeTypeName("__AnonymousRecord_msquic_L1080_C9")]
        public _NEW_CONNECTION_e__Struct NEW_CONNECTION;

        [FieldOffset(0)]
        [NativeTypeName("__AnonymousRecord_msquic_L1084_C9")]
        public _STOP_COMPLETE_e__Struct STOP_COMPLETE;

        public unsafe partial struct _NEW_CONNECTION_e__Struct
        {
            [NativeTypeName("const QUIC_NEW_CONNECTION_INFO *")]
            public QUIC_NEW_CONNECTION_INFO* Info;

            [NativeTypeName("HQUIC")]
            public QUIC_HANDLE* Connection;
        }

        public partial struct _STOP_COMPLETE_e__Struct
        {
            public byte _bitfield;

            [NativeTypeName("BOOLEAN : 1")]
            public byte AppCloseInProgress
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return (byte)(_bitfield & 0x1u);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    _bitfield = (byte)((_bitfield & ~0x1u) | (value & 0x1u));
                }
            }

            [NativeTypeName("BOOLEAN : 7")]
            public byte RESERVED
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return (byte)((_bitfield >> 1) & 0x7Fu);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    _bitfield = (byte)((_bitfield & ~(0x7Fu << 1)) | ((value & 0x7Fu) << 1));
                }
            }
        }
    }
}

using System;

namespace AKNet.MSQuicWrapper;

public unsafe partial struct QUIC_SCHANNEL_CONTEXT_ATTRIBUTE_EX_W
{
    [NativeTypeName("unsigned long")]
    public UIntPtr Attribute;

    [NativeTypeName("unsigned long")]
    public UIntPtr BufferLength;

    public void* Buffer;
}

using System;

namespace AKNet.MSQuicWrapper;

public unsafe partial struct QUIC_SCHANNEL_CONTEXT_ATTRIBUTE_W
{
    [NativeTypeName("unsigned long")]
    public UIntPtr Attribute;

    public void* Buffer;
}

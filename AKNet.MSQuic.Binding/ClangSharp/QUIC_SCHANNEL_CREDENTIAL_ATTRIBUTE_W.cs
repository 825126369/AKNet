using System;

namespace AKNet.MSQuicWrapper;

public unsafe partial struct QUIC_SCHANNEL_CREDENTIAL_ATTRIBUTE_W
{
    [NativeTypeName("unsigned long")]
    public UIntPtr Attribute;

    [NativeTypeName("unsigned long")]
    public UIntPtr BufferLength;

    public void* Buffer;
}

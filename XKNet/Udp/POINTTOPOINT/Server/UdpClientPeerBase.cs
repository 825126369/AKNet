using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    public interface UdpClientPeerBase
    {
        void Reset();
        void Release();
    }
}

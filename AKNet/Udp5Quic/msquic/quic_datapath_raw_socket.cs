using System;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        static void RawResolveRouteComplete(object Context, CXPLAT_ROUTE Route, byte[] PhysicalAddress, byte PathId)
        {
            QUIC_CONNECTION Connection = (QUIC_CONNECTION)Context;
            Array.Copy(PhysicalAddress, Route.NextHopLinkLayerAddress, Route.NextHopLinkLayerAddress.Length);
            Route.State =  CXPLAT_ROUTE_STATE.RouteResolved;
        }
    }
}
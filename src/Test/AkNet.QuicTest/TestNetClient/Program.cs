using AKNet.Common;
using AKNet.Extentions.Protobuf;
using Google.Protobuf;

namespace TestNetClient
{
    public class NetHandler : QuicTestClientBase
    {
        public override QuicClientMainBase Create()
        {
            return new NetClientMain(NetType.MSQuic);
        }

        public override void OnTestFinish()
        {
           udp_statistic.PrintInfo();
        }
    }

    public static class ClientPeerBaseExtentions
    {
        public static void SendNetData(this QuicClientPeerBase mInterface, byte nStreamIndex, ushort nPackageId, IMessage data)
        {
            if (mInterface.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ReadOnlySpan<byte> stream = Proto3Tool.SerializePackage(data);
                mInterface.SendNetData(nStreamIndex, nPackageId, stream);
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var mTest = new NetHandler();
            mTest.Start();
        }
    }
}

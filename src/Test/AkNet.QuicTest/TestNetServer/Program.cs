using AKNet.Common;

namespace TestNetServer
{
    public class NetHandler : QuicTestServerBase
    {
        public override QuicServerMainBase Create()
        {
            return new NetServerMain(NetType.Quic);
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

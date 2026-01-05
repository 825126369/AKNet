using AKNet.Common;

namespace TestNetServer
{
    public class NetHandler : NetTestServerBase
    {
        public override NetServerMainBase Create()
        {
            return new NetServerMain(NetType.Udp2MSQuic);
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

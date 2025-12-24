using AKNet.Common;

namespace TestNetClient
{
    public class NetHandler : NetTestClientBase
    {
        public override NetClientMainBase Create()
        {
            return new NetClientMain(NetType.Udp1MSQuic);
        }

        public override void OnTestFinish()
        {
            udp_statistic.PrintInfo();
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

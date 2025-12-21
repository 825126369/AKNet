using AKNet.Common;

namespace TestNetClient
{
    public class NetHandler : NetTestClientBase
    {
        public override NetClientMainBase Create()
        {
            return new NetClientMain(NetType.TCP);
        }

        public override void OnTestFinish()
        {
            
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

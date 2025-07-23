using AKNet.Common;
using Google.Protobuf;
using TestCommon;

namespace TestNetClient
{
    public static class ClientPeerBaseExtentions
    {
        public static void SendNetData(this ClientPeerBase mInterface, ushort nPackageId, IMessage data)
        {
            if (mInterface.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ReadOnlySpan<byte> stream = Protocol3Utility.SerializePackage(data);
                mInterface.SendNetData(nPackageId, stream);
            }
        }
    }

    internal class Program
    {
        static NetHandler mTest = null;
        static void Main(string[] args)
        {
            mTest = new NetHandler();
            mTest.Init();
            UpdateMgr.Do(Update);
        }

        static void Update(double fElapsed)
        {
            if (fElapsed >= 0.3)
            {
                Console.WriteLine("TestUdpClient 帧 时间 太长: " + fElapsed);
            }

            mTest.Update(fElapsed);
        }
    }
}

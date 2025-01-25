using AKNet.Common;

namespace OtherTest
{
    internal class CheckSumTest
    {
        public void Test()
        {
            byte[] data = { 0x01, 0x02, 0x03, 0x04, 0x05 };
            ushort checksum = CheckSumHelper.ComputeTcpUdpChecksum(data, data.Length, 1100, 300000, 6);
            Console.WriteLine($"校验和: {checksum:X4}");
        }
    }
}

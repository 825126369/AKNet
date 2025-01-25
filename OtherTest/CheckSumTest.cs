using AKNet.Common;

namespace OtherTest
{
    internal class CheckSumTest
    {
        public void Test()
        {
            byte[] data = { 0x01, 0x02, 0x03, 0x04, 0x05 };
            ushort previousChecksum = 0x1234;
            ushort checksum = CheckSumHelper.CsumPartial(data, data.Length, previousChecksum);
            Console.WriteLine($"校验和: {checksum:X4}");
        }
    }
}

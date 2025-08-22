namespace AKNet.QuicNet.Common
{
    public class MaxDataFrame : Frame
    {
        public override byte Type => 0x10;
        public VariableInteger MaximumData { get; set; }

        public override void Decode(ByteArray array)
        {
            array.ReadByte();
            MaximumData = array.ReadVariableInteger();
        }

        public override byte[] Encode()
        {
            List<byte> result = new List<byte>();

            result.Add(Type);
            result.AddRange(MaximumData.ToByteArray());

            return result.ToArray();
        }
    }
}

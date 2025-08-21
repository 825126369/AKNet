namespace AKNet.QuicNet.Common
{
    public class AckFrame : Frame
    {
        public override byte Type => 0x02;

        public override void Decode(ByteArray array)
        {
            throw new NotImplementedException();
        }

        public override byte[] Encode()
        {
            throw new NotImplementedException();
        }
    }
}

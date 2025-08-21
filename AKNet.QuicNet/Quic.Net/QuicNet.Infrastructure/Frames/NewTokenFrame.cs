namespace AKNet.QuicNet.Common
{
    public class NewTokenFrame : Frame
    {
        public override byte Type => 0x07;

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

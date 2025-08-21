namespace AKNet.QuicNet.Common
{
    public class PathResponseFrame : Frame
    {
        public override byte Type => 0x1b;

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

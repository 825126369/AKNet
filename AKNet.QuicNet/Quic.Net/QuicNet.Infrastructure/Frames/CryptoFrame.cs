namespace AKNet.QuicNet.Common
{
    public class CryptoFrame : Frame
    {
        public override byte Type => 0x06;

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

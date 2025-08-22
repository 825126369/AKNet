namespace AKNet.QuicNet.Common
{
    public class StopSendingFrame : Frame
    {
        public override byte Type => 0x05;

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

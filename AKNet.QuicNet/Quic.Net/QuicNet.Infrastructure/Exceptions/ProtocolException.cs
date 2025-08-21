namespace AKNet.QuicNet.Common
{
    public class ProtocolException : Exception
    {
        public ProtocolException()
        {
        }

        public ProtocolException(string message) : base(message)
        {
        }
    }
}

namespace AKNet.QuicNet.Common
{
    public class QuicListenerNotStartedException : Exception
    {
        public QuicListenerNotStartedException() { }

        public QuicListenerNotStartedException(string message) : base(message)
        {
        }
    }
}

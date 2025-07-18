namespace AKNet.Platform.Socket
{
    public abstract class EndPoint
    {
        public virtual AddressFamily AddressFamily
        {
            get
            {
                throw new Exception();
            }
        }

        public virtual SocketAddress Serialize()
        {
            throw new Exception();
        }
        
        public virtual EndPoint Create(SocketAddress socketAddress)
        {
            throw new Exception();
        }
    }
}

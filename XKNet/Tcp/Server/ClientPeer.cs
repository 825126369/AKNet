namespace XKNet.Tcp.Server
{
    public class ClientPeer : SocketSendPeer
	{
		public ClientPeer(ServerBase mNetServer):base(mNetServer)
        {
				
        }

		public override void Update(double elapsed)
		{
			base.Update(elapsed);
		}
	}
}



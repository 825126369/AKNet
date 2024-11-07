using LiteNetLib;
using LiteNetLib.Utils;

namespace TestOtherUdpServer
{
    internal class NetHandler
    {
        EventBasedNetListener listener = new EventBasedNetListener();
        NetManager server;

        public void Do()
        {
            server = new NetManager(listener);
            server.Start(9050);
            listener.ConnectionRequestEvent += request =>
            {
                if (server.ConnectedPeersCount < 10 /* max connections */)
                    request.AcceptIfKey("SomeConnectionKey");
                else
                    request.Reject();
            };

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("We got connection: {0}", peer);  // Show peer ip
                NetDataWriter writer = new NetDataWriter();         // Create writer class
                writer.Put("Hello client!");                        // Put some string
                peer.Send(writer, DeliveryMethod.ReliableOrdered);  // Send with reliability
            };

            listener.NetworkReceiveEvent += ReceiveMessage;
        }

        public void Update()
        {
            server.PollEvents();
        }

        void ReceiveMessage(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            if(reader == null) return;
            try
            {
                var Data = reader.GetString(1024 * 1024);

                Console.WriteLine("We got connection: {0}", peer);  // Show peer ip
                NetDataWriter writer = new NetDataWriter();         // Create writer class
                writer.Put(Data);                        // Put some string
                peer.Send(writer, DeliveryMethod.ReliableOrdered);  // Send with reliability
            }
            catch (Exception ex)
            {
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}

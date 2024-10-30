# XKNet
这是一个包括 TCP，UDP，Protobuf 封装的拿来就能用的 游戏网络库

'''Example:

 public class NetServerHandler
 {
     TcpNetServerMain mNetServer = null;
     public void Init()
     {
         mNetServer = new TcpNetServerMain();
         mNetServer.addNetListenFun(NetProtocolCommand.CS_REQUEST_LOGIN, receive_csRequestLogin);
         mNetServer.InitNet(nPort);
     }

     public void Update(double fElapsedTime)
     {
         mNetServer.Update(fElapsedTime);
     }

     void receive_csRequestLogin(ClientPeerBase clientPeer, NetPackage mNetPackage)
     {
         packet_cs_Login mReceiveMsg = Protocol3Utility.getData<packet_cs_Login>(mNetPackage);
         LoginServerMgr.Instance.HandleLogin(clientPeer, mReceiveMsg);
     }
 }

  public class NetClientHandler
   {
       public TcpNetClientMain mNetClient = null;
       public void Init()
       {
           mNetClient = new TcpNetClientMain();
           mNetClient.addNetListenFun(NetProtocolCommand.SGG_SERVER_INFO_RESULT, receive_selectGateServerInfo);
           mNetClient.ConnectServer(Ip, nPort);
       }

       public void Update(double fElapsedTime)
       {
           mNetClient.Update(fElapsedTime);
       }

       private void receive_selectGateServerInfo(ClientPeerBase clientPeer, NetPackage package)
       {
           packet_sgg_SendServerInfo_Result mSendMsg = Protocol3Utility.getData<packet_sgg_SendServerInfo_Result>(package);
           GateServerMgr.Instance.InitDbInfo(mSendMsg.MServerInfo);
           IMessagePool<packet_sgg_SendServerInfo_Result>.recycle(mSendMsg);
       }
   }

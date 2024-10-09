//using System;
//using UnityEngine;
//using xk_System.Net.UDP.POINTTOPOINT.Server;
//using Google.Protobuf;
//using UdpPointtopointProtocols;
//using xk_System.Net.UDP.POINTTOPOINT.Protocol;
//using xk_System.Net.UDP.POINTTOPOINT;

//public class UdpServerTest : MonoBehaviour
//{
//    NetServer mNetServer = new NetServer();

//    public const bool InTest = true;
//    private void Start()
//    {
//        mNetServer.GetPackageManager().addNetListenFun(UdpNetCommand.COMMAND_TESTCHAT, ReceiveMessage);
//        mNetServer.InitNet("0.0.0.0", 10001);
//    }

//    private void Update()
//    {
//        mNetServer.Update(Time.deltaTime);
//    }

//    void OnDestroy()
//    {
//        mNetServer.Release();
//    }

//    private void ReceiveMessage(ClientPeer peer, NetPackage mPackage)
//    {
//        //Debug.Log("Server 收到数据: " + mPackage.nOrderId + " | " + mPackage.Length + " | " + mPackage.buffer.Length);
//        TESTChatMessage mdata = Protocol3Utility.getData<TESTChatMessage>(mPackage);
//        peer.SendNetData(UdpNetCommand.COMMAND_TESTCHAT, mdata);
//        ProtobufHelper.IMessagePool<TESTChatMessage>.recycle(mdata);
//    }
//}


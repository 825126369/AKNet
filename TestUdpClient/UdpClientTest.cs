//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using xk_System.Net.UDP.POINTTOPOINT.Client;
//using Google.Protobuf;
//using UdpPointtopointProtocols;
//using xk_System.Net.UDP.POINTTOPOINT.Protocol;
//using xk_System.Net.UDP.POINTTOPOINT;

//public class UdpClientTest : MonoBehaviour
//{
//    public int nClientCount = 100;
//    public int nPackageCount = 10;
//    List<NetClient> mClientList = new List < NetClient >();
    
//    System.Random mRandom = new System.Random();
//    private void Start()
//	{
//        for (int i = 0; i < nClientCount; i++)
//        {
//			NetClient mNetClient = new NetClient();
//			mClientList.Add(mNetClient);

//            mNetClient.addNetListenFun(UdpNetCommand.COMMAND_TESTCHAT, ReceiveMessage);
//            mNetClient.ConnectServer(LuaUtils.Instance.GetLocalNetIp(), 10001);
//        }
//    }

//    void Update()
//    {
//        for (int i = 0; i < nClientCount; i++)
//        {
//            NetClient v = mClientList[i];
//            NetClient mNetClient = v;
//            mNetClient.Update(Time.deltaTime);

//            for (int j = 0; j < nPackageCount; j++)
//            {
//                TESTChatMessage mdata = ProtobufHelper.IMessagePool<TESTChatMessage>.Pop();
//                mdata.Id = (uint)(i + 1);
//                //if (mRandom.Next(1, 3) == 1)
//                //{
//                //    mdata.TalkMsg = "Begins..........End";
//                //}
//                //else
//                //{
//                //    mdata.TalkMsg = "Begin。。。。。。。。。。。。....................................................................................................................................................................................................sdfsfsf.s.fsfsfds.df.s.fwqerqweprijqwperqwerqowheropwheporpwerjpo qjwepowiopeqwoerpowqejoqwejoqwjeo   qweopqwjeop opqweuq opweuo  eqwup   quweopiquowequoewuqowe.........................................End";
//                //}
//                mNetClient.SendNetData(UdpNetCommand.COMMAND_TESTCHAT, mdata);
//                ProtobufHelper.IMessagePool<TESTChatMessage>.recycle(mdata);
//            }
//        }
//    }

//    void OnDestroy()
//    {
//        foreach (var v in mClientList)
//        {
//            v.Release();
//        }
//    }
    
//	void ReceiveMessage(ClientPeer peer, NetPackage mPackage)
//	{
//        TESTChatMessage mdata = Protocol3Utility.getData<TESTChatMessage>(mPackage);
//		Debug.Assert (mdata != null);
//        Debug.Log("Client 收到数据: " + mPackage.nOrderId + " | " + mdata.Id);
//        ProtobufHelper.IMessagePool<TESTChatMessage>.recycle(mdata);
//    }
//}


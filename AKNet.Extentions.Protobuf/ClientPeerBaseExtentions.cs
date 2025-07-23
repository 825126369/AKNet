/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:04
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

//using AKNet.Common;
//using Google.Protobuf;
//using System;

//namespace AKNet.Extentions.Protobuf
//{
//    public static class ClientPeerBaseExtentions
//    {
//        public static void SendNetData(this ClientPeerBase mInterface, ushort nPackageId, IMessage data)
//        {
//            if (mInterface.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
//            {
//                ReadOnlySpan<byte> stream = Protocol3Utility.SerializePackage(data);
//                mInterface.SendNetData(nPackageId, stream);
//            }
//        }
//    }
//}

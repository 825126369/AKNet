/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

using AKNet.Common;
using Google.Protobuf;
using System;

namespace AKNet.Extentions.Protobuf
{
    public static class ClientPeerBaseExtentions
    {
        public static void SendNetData(this ClientPeerBase mInterface, ushort nPackageId, IMessage data)
        {
            if (mInterface.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                ReadOnlySpan<byte> stream = Proto3Tool.SerializePackage(data);
                mInterface.SendNetData(nPackageId, stream);
            }
        }
    }
}

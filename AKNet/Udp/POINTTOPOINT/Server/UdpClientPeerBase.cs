﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal interface UdpClientPeerBase
    {
        void SetName(string Name);
        void Reset();
    }
}

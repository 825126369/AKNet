﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    public class NetClientMain : NetClientMainBase
    {
        public NetClientMain()
        {
            mInterface = new AKNet.Udp1MSQuic.Client.QuicNetClientMain();
        }

        public NetClientMain(NetType nNetType) :base(nNetType)
        {
            
        }
    }
}

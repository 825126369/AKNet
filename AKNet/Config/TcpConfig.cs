﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:22
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    public class TcpConfig: NetConfigInterface
    {
        public double fReceiveHeartBeatTimeOut = 5.0;
        public double fMySendHeartBeatMaxTime = 2.0;
        public double fReConnectMaxCdTime = 3.0;
        //Server
        public int MaxPlayerCount = 10000;
        //加解密
        public ECryptoType nECryptoType = ECryptoType.None;
        public string CryptoPasswrod1 = string.Empty;
        public string CryptoPasswrod2 = string.Empty;
    }
}

/************************************Copyright*****************************************
*        ProjectName:XKNet
*        Web:https://github.com/825126369/XKNet
*        Description:XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 12:14:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace XKNet.Common
{
    internal interface ServerBase
	{
        void InitNet();
        void InitNet(int nPort);
        void InitNet(string Ip, int nPort);
        int GetPort();
        SOCKET_SERVER_STATE GetServerState();
        void Update(double elapsed);
        void addNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func);
        void removeNetListenFun(UInt16 id, Action<ClientPeerBase, NetPackage> func);
        void SetNetCommonListenFun(Action<ClientPeerBase, NetPackage> func);
        void Release();
        void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc);
        void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc);
    }
}

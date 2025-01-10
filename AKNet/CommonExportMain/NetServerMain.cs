/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:22
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Common
{
    public class NetServerMain : NetServerMainBase
    {
        public NetServerMain()
        {
            mInterface = new AKNet.Udp.POINTTOPOINT.Server.UdpNetServerMain();
        }

        public NetServerMain(NetType nNetType) : base(nNetType)
        {

        }

        public NetServerMain(NetConfigInterface IConfig) : base(IConfig)
        {

        }
    }
}

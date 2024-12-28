/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:22
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Extentions.Protobuf
{
    public class NetClientMain : NetClientMainBase
    {
        public NetClientMain()
        {
            mInterface = new AKNet.Udp.POINTTOPOINT.Client.UdpNetClientMain();
        }

        public NetClientMain(NetType nNetType) :base(nNetType)
        {
            
        }

        public NetClientMain(NetConfigInterface IConfig):base(IConfig)
        {
           
        }
    }
}

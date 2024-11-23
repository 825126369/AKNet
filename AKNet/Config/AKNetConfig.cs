/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:34
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;

namespace AKNet.Common
{
    public static class AKNetConfig
    {
        public static int nIMessagePoolDefaultMaxCapacity = 0;
        public static void SetIMessagePoolMaxCapacity<T>(int nMaxCapacity) where T : class, IMessage, IMessage<T>, IProtobufResetInterface, new()
        {
            IMessagePool<T>.SetMaxCapacity(nMaxCapacity);
        }
    }
}

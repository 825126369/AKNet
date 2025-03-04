/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class ObjectPoolManager
    {
        private readonly SafeObjectPool<sk_buff> mSkbPool = null;
        public ObjectPoolManager()
        {
            mSkbPool = new SafeObjectPool<sk_buff>(1024);
        }

        public sk_buff Skb_Pop()
        {
            return mSkbPool.Pop();
        }

        public void Skb_Recycle(sk_buff skb)
        {
            mSkbPool.recycle(skb);
        }

    }
}

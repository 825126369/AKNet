/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:57
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace AKNet.LinuxTcp.Common
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

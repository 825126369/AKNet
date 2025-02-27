/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections.Generic;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class sk_backlog
    {
        public LinkedList<sk_buff> mQueue = new LinkedList<sk_buff>();
        public long rmem_alloc;
        public int len;

        public sk_buff head
        {
            get 
            {
                if (mQueue.First != null)
                {
                    return mQueue.First.Value;
                }
                return null;
            }
        }

        public sk_buff tail
        {
            get 
            {
                if (mQueue.Last != null)
                {
                    return mQueue.Last.Value;
                }

                return null;
            }
        }
    }
}

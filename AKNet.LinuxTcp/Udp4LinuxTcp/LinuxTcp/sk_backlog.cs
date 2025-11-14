/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:41
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

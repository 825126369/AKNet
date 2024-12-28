/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:24
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections.Generic;

namespace AKNet.LinuxTcp
{
    internal class sk_backlog
    {
        public LinkedList<sk_buff> mQueue = new LinkedList<sk_buff>();
        public long rmem_alloc;
        public int len;
    }
}

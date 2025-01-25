/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:24
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp
{
    internal class sk_buff_list
    {
        public sk_buff next;
        public sk_buff prev;
    }

    internal class sk_buff_head:sk_buff
    {
	    public uint qlen;
    }

    public class list_head<T>
    {
        public T value;
        public list_head<T> next;
        public list_head<T> prev;
    }

}

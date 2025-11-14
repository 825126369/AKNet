/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:32
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace MSQuic1
{
    internal static partial class MSQuicFunc
    {
        public static T CreateInstance<T>() where T : class, new()
        {
            try
            {
                T instance = new T();
                return instance;
            }
            catch
            {
                
            }
            return null;
        }
    }
}

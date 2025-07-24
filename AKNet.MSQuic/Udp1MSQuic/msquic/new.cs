namespace AKNet.Udp1MSQuic.Common
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

namespace XKNet.Common
{
    /// <summary>
    /// 如果实现单例，就继承这个类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class Singleton<T> where T : class, new()
    {
        protected Singleton()
        {
            NetLog.Assert(instance == null, "单例模式, 不可以再 New(): " + this.GetType().ToString());
        }

        private static T instance = new T();
        public static T Instance
        {
            get
            {
                return instance;
            }
        }
    }
}


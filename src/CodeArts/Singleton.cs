using System;

namespace CodeArts
{
    /// <summary>
    /// 单例封装类。
    /// </summary>
    /// <typeparam name="T">基类类型。</typeparam>
    public class Singleton<T> where T : class
    {
        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static Singleton() { }

        /// <summary>
        /// 单例。
        /// </summary>
        public static T Instance => Nested.Instance;

        private static class Nested
        {
            static Nested() => Instance = RuntimeServPools.Singleton<T>();

            public static readonly T Instance;
        }
    }
}

using System;

namespace CodeArts.DesignMode
{
    /// <summary>
    /// 单例基类
    /// </summary>
    /// <typeparam name="T">单例类型</typeparam>
    public class Singleton<T> where T : Singleton<T>
    {
        /// <summary>
        /// 静态构造函数
        /// </summary>
        static Singleton() { }

        /// <summary>
        /// 单例
        /// </summary>
        public static T Instance => Nested.Instance;

        /// <summary>
        /// 内嵌的
        /// </summary>
        private static class Nested
        {
            /// <summary>
            /// 静态构造函数
            /// </summary>
            static Nested() { }

            /// <summary>
            /// 单例
            /// </summary>
            public static readonly T Instance = (T)Activator.CreateInstance(typeof(T), true);
        }
    }
}

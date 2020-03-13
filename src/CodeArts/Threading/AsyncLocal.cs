using System.Collections.Concurrent;
using System.Security;

namespace System.Threading
{
#if NET40 || NET45 || NET451 || NET452
    /// <summary>
    /// 异步对象（数据在每个线程中唯一。）
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public sealed class AsyncLocal<T>
    {
        [SecurityCritical]
        private readonly Action<AsyncLocalValueChangedArgs<T>> m_valueChangedHandler;

        private static readonly ConcurrentDictionary<Thread, T> mapperCache = new ConcurrentDictionary<Thread, T>();

        /// <summary>
        /// 数据值
        /// </summary>
        public T Value
        {
            [SecuritySafeCritical]
            get => mapperCache.TryGetValue(Thread.CurrentThread, out T value) ? value : default;
            [SecuritySafeCritical]
            set => mapperCache.AddOrUpdate(Thread.CurrentThread, value, (thread, oldValue) =>
            {
                if (m_valueChangedHandler is null || Equals(oldValue, value)) return value;

                m_valueChangedHandler.Invoke(new AsyncLocalValueChangedArgs<T>(oldValue, value, false));

                return value;
            });
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public AsyncLocal() { }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="valueChangedHandler">数据改变事件</param>
        public AsyncLocal(Action<AsyncLocalValueChangedArgs<T>> valueChangedHandler) => m_valueChangedHandler = valueChangedHandler;
    }
#endif
}

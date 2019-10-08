using System.Collections.Concurrent;
using System.Security;

namespace System.Threading
{
#if NET40 || NET45 || NET451 || NET452
    //
    // 摘要:
    //     Represents ambient data that is local to a given asynchronous control flow, such
    //     as an asynchronous method.
    //
    // 类型参数:
    //   T:
    //     The type of the ambient data.
    public sealed class AsyncLocal<T>
    {
        [SecurityCritical]
        private readonly Action<AsyncLocalValueChangedArgs<T>> m_valueChangedHandler;

        private readonly static ConcurrentDictionary<Thread, T> mapperCache = new ConcurrentDictionary<Thread, T>();

        //
        // 摘要:
        //     Gets or sets the value of the ambient data.
        //
        // 返回结果:
        //     The value of the ambient data.
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

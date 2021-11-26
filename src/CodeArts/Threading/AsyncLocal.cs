#if NET40 || NET45
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security;

namespace System.Threading
{
    /// <summary>
    /// 异步对象（数据在每个线程中唯一。）
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    public sealed class AsyncLocal<T>
    {
        private static bool _clearTimerRun;
        private static readonly System.Timers.Timer _clearTimer;

        [SecurityCritical]
        private readonly Action<AsyncLocalValueChangedArgs<T>> m_valueChangedHandler;

        private static readonly ConcurrentDictionary<Thread, T> localCache = new ConcurrentDictionary<Thread, T>();

        /// <summary>
        /// 数据值。
        /// </summary>
        public T Value
        {
            [SecuritySafeCritical]
            get => localCache.TryGetValue(Thread.CurrentThread, out T value) ? value : default;
            [SecuritySafeCritical]
            set
            {
                localCache.AddOrUpdate(Thread.CurrentThread, value, (thread, oldValue) =>
                {
                    if (m_valueChangedHandler is null || Equals(oldValue, value)) return value;

                    m_valueChangedHandler.Invoke(new AsyncLocalValueChangedArgs<T>(oldValue, value, false));

                    return value;
                });

                if (!_clearTimerRun)
                {
                    _clearTimerRun = true;
                    _clearTimer.Start();
                }
            }
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        public AsyncLocal() { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="valueChangedHandler">数据改变事件。</param>
        public AsyncLocal(Action<AsyncLocalValueChangedArgs<T>> valueChangedHandler) => m_valueChangedHandler = valueChangedHandler;

        static AsyncLocal()
        {
            _clearTimer = new System.Timers.Timer(1000D * 60D);
            _clearTimer.Elapsed += ClearTimerElapsed;
            _clearTimer.Enabled = true;
            _clearTimer.Stop();
            _clearTimerRun = false;
        }

        private static void ClearTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var threads = new List<Thread>(Math.Min(localCache.Count, 4));

            foreach (var kv in localCache)
            {
                if (kv.Key.IsAlive)
                {
                    continue;
                }

                threads.Add(kv.Key);
            }

            if (threads.Count == 0)
            {
                return;
            }

            lock (localCache)
            {
                foreach (var thread in threads)
                {
                    localCache.TryRemove(thread, out _);
                }

                if (localCache.Count == 0)
                {
                    _clearTimerRun = false;

                    _clearTimer.Stop();
                }
            }
        }
    }
}
#endif

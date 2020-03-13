using System.Collections.Concurrent;

namespace System.Threading
{
    /// <summary>
    /// 线程安全
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public static class CallContext<T>
    {
        private static readonly ConcurrentDictionary<string, AsyncLocal<T>> threadCache = new ConcurrentDictionary<string, AsyncLocal<T>>();

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="data">数据</param>
        public static void SetData(string name, T data) => threadCache.GetOrAdd(name, _ => new AsyncLocal<T>()).Value = data;

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static T GetData(string name) => threadCache.TryGetValue(name, out AsyncLocal<T> localCache) ? localCache.Value : default;

        /// <summary>
        /// 清除数据
        /// </summary>
        /// <param name="name">名称</param>
        public static void FreeNamedDataSlot(string name)
        {
            if (threadCache.TryGetValue(name, out AsyncLocal<T> localCache))
            {
                localCache.Value = default;
            }
        }
    }
}

#if NET40
namespace System.Collections.Generic
{
    /// <summary>
    /// 表示元素的强类型化只读集合。
    /// </summary>
    /// <typeparam name="T">元素的类型。</typeparam>
    public interface IReadOnlyCollection<out T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// 获取集合中的元素数。
        /// </summary>
        int Count { get; }
    }
}
#endif
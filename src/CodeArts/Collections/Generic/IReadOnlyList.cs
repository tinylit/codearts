#if NET40
namespace System.Collections.Generic
{
    /// <summary>
    /// 表示可按照索引进行访问的元素的只读集合。
    /// </summary>
    /// <typeparam name="T">只读列表中元素的类型</typeparam>
    public interface IReadOnlyList<out T> : IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// 获取位于只读列表中指定索引处的元素。
        /// </summary>
        /// <param name="index">要获取的元素的索引(索引从零开始)。</param>
        /// <returns>在只读列表中指定索引处的元素。</returns>
        T this[int index] { get; }
    }
}
#endif
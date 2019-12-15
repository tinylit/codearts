using CodeArts;

namespace System.Linq
{
    /// <summary>
    /// 查询器扩展
    /// </summary>
    public static class QueryableExtentions
    {
        /// <summary>
        /// 转分页数据
        /// </summary>
        /// <typeparam name="T">源</typeparam>
        /// <param name="source">源</param>
        /// <param name="page">页码</param>
        /// <param name="size">分页条数</param>
        /// <returns></returns>
        public static PagedList<T> ToList<T>(this IQueryable<T> source, int page, int size)
            => new PagedList<T>(source, page, size);

    }
}

using CodeArts;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Linq
{
    /// <summary>
    /// 查询器扩展
    /// </summary>
    public static class QueryableExtentions
    {
        /// <summary>
        /// 转为集合。
        /// </summary>
        /// <typeparam name="T">源</typeparam>
        /// <param name="source">源</param>
        /// <param name="predicate">条件</param>
        /// <returns></returns>
        public static List<T> ToList<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
            => source.Where(predicate).ToList();

        /// <summary>
        /// 转分页数据
        /// </summary>
        /// <typeparam name="T">源</typeparam>
        /// <param name="source">源</param>
        /// <param name="page">页码（索引从“0”开始）</param>
        /// <param name="size">分页条数</param>
        /// <returns></returns>
        public static PagedList<T> ToList<T>(this IQueryable<T> source, int page, int size)
            => new PagedList<T>(source, page, size);

    }
}

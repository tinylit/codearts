using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts
{
    /// <summary>
    /// 分页的列表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagedList<T> : IEnumerable<T>, IEnumerable
    {
        private readonly IQueryable<T> list;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="queryable">查询能力</param>
        /// <param name="page">页码</param>
        /// <param name="size">分页条数</param>
        public PagedList(IQueryable<T> queryable, int page, int size)
        {
            list = queryable ?? throw new ArgumentNullException(nameof(queryable));
            if (page < 1)
                throw new IndexOutOfRangeException("页码不能小于1。");
            Page = page;
            if (Size < 1)
                throw new IndexOutOfRangeException("分页条目不能小于1。");
            Size = size;
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int Page { get; }
        /// <summary>
        /// 分页条数
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// 总数
        /// </summary>
        public int Count => list.Count();

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() => list.Skip(Size * (Page - 1)).Take(Size).GetEnumerator();

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

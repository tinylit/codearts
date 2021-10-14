using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts
{
    /// <summary>
    /// 分页的列表。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public sealed class PagedList<T> : IEnumerable<T>, IEnumerable
    {
        private readonly IEnumerable<T> datas;

        /// <summary>
        /// 空集合。
        /// </summary>
        public static readonly PagedList<T> Empty = new PagedList<T>();

        /// <summary>
        /// 空集合。
        /// </summary>
        public PagedList() => datas = Enumerable.Empty<T>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="queryable">查询能力。</param>
        /// <param name="pageIndex">页码（索引从0开始）。</param>
        /// <param name="pageSize">分页条数。</param>
        public PagedList(IQueryable<T> queryable, int pageIndex, int pageSize)
        {
            if (pageIndex < 0)
            {
                throw new IndexOutOfRangeException("页码不能小于0。");
            }
            if (pageSize < 1)
            {
                throw new IndexOutOfRangeException("分页条目不能小于1。");
            }

            var results = queryable.Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToList();

            if (pageIndex == 0 && pageSize > results.Count)
            {
                Count = results.Count;
            }
            else
            {
                Count = queryable.Count();
            }

            datas = results;

            PageIndex = pageIndex;

            PageSize = pageSize;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="datas">数据。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">分页条数。</param>
        /// <param name="totalCount">总数。</param>
        public PagedList(ICollection<T> datas, int pageIndex, int pageSize, int totalCount)
        {
            this.datas = datas ?? throw new ArgumentNullException(nameof(datas));

            if (pageIndex < 0)
            {
                throw new IndexOutOfRangeException("页码不能小于0。");
            }

            if (pageSize < 1)
            {
                throw new IndexOutOfRangeException("分页条目不能小于1。");
            }

            if (totalCount < 0)
            {
                throw new IndexOutOfRangeException("总数不能小于0。");
            }

            if (datas.Count > pageSize)
            {
                throw new IndexOutOfRangeException("集合元素总数不能大于分页条数。");
            }

            if (datas.Count > totalCount)
            {
                throw new IndexOutOfRangeException("集合元素总数不能大于总条数。");
            }

            PageIndex = pageIndex;

            PageSize = pageSize;

            Count = totalCount;
        }

        /// <summary>
        /// 当前页码（索引从0开始）。
        /// </summary>
        public int PageIndex { get; }

        /// <summary>
        /// 分页条数。
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// 总数。
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 获取迭代器。
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() => datas.GetEnumerator();

        /// <summary>
        /// 获取迭代器。
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

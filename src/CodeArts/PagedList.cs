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
    public class PagedList<T> : IEnumerable<T>, IEnumerable
    {
        private readonly bool isEnumerable = false;

        private readonly int totalCount = 0;

        private readonly IQueryable<T> queryable;

        private readonly IEnumerable<T> enumerable;

        /// <summary>
        /// 空集合。
        /// </summary>
        public static readonly PagedList<T> Empty = new PagedList<T>();

        /// <summary>
        /// 空集合。
        /// </summary>
        public PagedList()
        {
            isEnumerable = true;

            enumerable = Enumerable.Empty<T>();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="queryable">查询能力。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">分页条数。</param>
        public PagedList(IQueryable<T> queryable, int pageIndex, int pageSize)
        {
            this.queryable = queryable ?? throw new ArgumentNullException(nameof(queryable));

            if (pageIndex < 0)
            {
                throw new IndexOutOfRangeException("页码不能小于0。");
            }
            if (pageSize < 1)
            {
                throw new IndexOutOfRangeException("分页条目不能小于1。");
            }

            PageIndex = pageIndex;

            PageSize = pageSize;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="enumerable">数据。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">分页条数。</param>
        /// <param name="totalCount">总数。</param>
        public PagedList(ICollection<T> enumerable, int pageIndex, int pageSize, int totalCount)
        {
            this.enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));

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

            if (enumerable.Count > pageSize)
            {
                throw new IndexOutOfRangeException("集合元素总数不能大于分页条数。");
            }

            if (enumerable.Count > totalCount)
            {
                throw new IndexOutOfRangeException("集合元素总数不能大于总条数。");
            }

            isEnumerable = true;

            PageIndex = pageIndex;

            PageSize = pageSize;

            this.totalCount = totalCount;
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
        public int Count => isEnumerable ? totalCount : queryable.Count();

        /// <summary>
        /// 获取迭代器。
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator<T> GetEnumerator() => isEnumerable ? enumerable.GetEnumerator() : queryable.Skip(PageSize * PageIndex).Take(PageSize).GetEnumerator();

        /// <summary>
        /// 获取迭代器。
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

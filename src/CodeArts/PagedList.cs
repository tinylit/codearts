using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts
{
    /// <summary>
    /// 空列表。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class EmptyList<T> : PagedList<T>
    {
        private static readonly IEnumerable<T> enumerable = Enumerable.Empty<T>();

        /// <summary>
        /// 固定值“0”。
        /// </summary>
        public override int Count => 0;

        /// <summary>
        /// 固定值，空数组。
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<T> GetEnumerator() => enumerable.GetEnumerator();
    }

    /// <summary>
    /// 分页的列表
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public class PagedList<T> : IEnumerable<T>, IEnumerable
    {
        private readonly IQueryable<T> queryable;

        /// <summary>
        /// 空集合。
        /// </summary>
        public static readonly PagedList<T> Empty = new EmptyList<T>();

        /// <summary>
        /// 私有构造函数
        /// </summary>
        protected PagedList() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="queryable">查询能力</param>
        /// <param name="page">页码</param>
        /// <param name="size">分页条数</param>
        public PagedList(IQueryable<T> queryable, int page, int size)
        {
            this.queryable = queryable ?? throw new ArgumentNullException(nameof(queryable));
            if (page < 0)
                throw new IndexOutOfRangeException("页码不能小于0。");
            Page = page;
            if (size < 1)
                throw new IndexOutOfRangeException("分页条目不能小于1。");
            Size = size;
        }

        /// <summary>
        /// 当前页码（索引从0开始）
        /// </summary>
        public int Page { get; }

        /// <summary>
        /// 分页条数
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// 总数
        /// </summary>
        public virtual int Count => queryable.Count();

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator<T> GetEnumerator() => queryable.Skip(Size * Page).Take(Size).GetEnumerator();

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

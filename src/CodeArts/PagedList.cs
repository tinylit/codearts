using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts
{
    /// <summary>
    /// 分页的列表
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public class PagedList<T> : IEnumerable<T>, IEnumerable
    {
        private readonly bool isEmpty;
        private readonly IQueryable<T> list;

        /// <summary>
        /// 空集合。
        /// </summary>
        public static readonly PagedList<T> Empty = new PagedList<T>();

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private PagedList() => isEmpty = true;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="queryable">查询能力</param>
        /// <param name="page">页码</param>
        /// <param name="size">分页条数</param>
        public PagedList(IQueryable<T> queryable, int page, int size)
        {
            list = queryable ?? throw new ArgumentNullException(nameof(queryable));
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
        public int Count => isEmpty ? 0 : list.Count();

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (isEmpty)
            {
                yield break;
            }
            else
            {
                foreach (T item in list.Skip(Size * Page).Take(Size))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

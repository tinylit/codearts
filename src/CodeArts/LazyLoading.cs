using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts
{
    /// <summary>
    /// 瀑布流。
    /// </summary>
    /// <typeparam name="T">类型。</typeparam>
    public class LazyLoading<T>
    {
        /// <summary>
        /// 空集合。
        /// </summary>
        public LazyLoading()
        {
#if NET40
            Datas = Enumerable.Empty<T>().ToReadOnlyList();
#else
            Datas = new List<T>(0);
#endif
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="queryable">查询能力。</param>
        /// <param name="offset">偏移量。</param>
        /// <param name="takeSize">获取条数。</param>
        public LazyLoading(IQueryable<T> queryable, int offset, int takeSize)
        {
            if (offset < 0)
            {
                throw new IndexOutOfRangeException("跳过数量不能小于0。");
            }
            if (takeSize < 1)
            {
                throw new IndexOutOfRangeException("获取条数不能小于1。");
            }

#if NET40
            Datas = queryable.Skip(offset)
                .Take(takeSize)
                .ToReadOnlyList();
#else
            Datas = queryable.Skip(offset)
                .Take(takeSize)
                .ToList();
#endif

            if (Datas.Count == takeSize)
            {
                HasNext = queryable
                    .Skip(Offset)
                    .Any();
            }

            Offset = offset + Datas.Count;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="datas">数据。</param>
        /// <param name="offset">偏移量。</param>
        /// <param name="hasNext">是否有下一条数据。</param>
        public LazyLoading(IEnumerable<T> datas, int offset, bool hasNext)
        {
            if (datas is null)
            {
                throw new ArgumentNullException(nameof(datas));
            }

            if (offset < 0)
            {
                throw new IndexOutOfRangeException("偏移量不能小于0。");
            }

#if NET40
            Datas = datas.ToReadOnlyList();
#else

            Datas = datas.ToList();
#endif

            HasNext = hasNext;

            Offset = offset + Datas.Count;
        }

        /// <summary>
        /// 下一页的偏移量。
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// 是否有下一个。
        /// </summary>
        public bool HasNext { get; set; }

        /// <summary>
        /// 数据。
        /// </summary>
        public IReadOnlyCollection<T> Datas { get; }
    }
}

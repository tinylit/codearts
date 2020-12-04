using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts.Db
{
    /// <summary>
    /// 仓储基本接口。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    public interface IRepository<T> : IOrderedQueryable<T>, IQueryable<T>, IEnumerable<T>, IOrderedQueryable, IQueryable, IEnumerable
    {
    }
}

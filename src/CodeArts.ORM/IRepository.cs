using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts.ORM
{
    /// <summary>
    /// 仓储基本接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRepository<T> : IOrderedQueryable<T>, IQueryable<T>, IEnumerable<T>, IOrderedQueryable, IQueryable, IEnumerable
    {

    }
}

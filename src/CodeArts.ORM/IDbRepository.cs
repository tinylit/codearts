﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts.ORM
{
    /// <summary>
    /// 仓储基本接口。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    public interface IDbRepository<T> : IRepository<T>, IOrderedQueryable<T>, IQueryable<T>, IEnumerable<T>, IEnumerable where T : class, IEntiy
    {
    }
}
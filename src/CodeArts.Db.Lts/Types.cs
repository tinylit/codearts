using System;
using System.Collections;
using System.Linq;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 类型。
    /// </summary>
    internal static class Types
    {
        /// <summary>
        /// <see cref="System.Linq.Enumerable"/>
        /// </summary>
        public static readonly Type Enumerable = typeof(Enumerable);

        /// <summary>
        /// <see cref="System.Linq.Queryable"/>
        /// </summary>
        public static readonly Type Queryable = typeof(Queryable);

        /// <summary>
        /// <see cref="System.Linq.IQueryable"/>
        /// </summary>
        public static readonly Type IQueryable = typeof(IQueryable);

        /// <summary>
        /// <see cref="System.Linq.RepositoryExtentions"/>
        /// </summary>
        public static readonly Type RepositoryExtentions = typeof(RepositoryExtentions);

        /// <summary>
        /// <see cref="string"/>
        /// </summary>
        public static readonly Type String = typeof(string);

        /// <summary>
        /// <see cref="System.Collections.IEnumerable"/>
        /// </summary>
        public static readonly Type IEnumerable = typeof(IEnumerable);
    }
}

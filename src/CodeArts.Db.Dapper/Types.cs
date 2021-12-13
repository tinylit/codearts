using System;
using System.Collections;
using System.Linq;

namespace CodeArts.Db
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
        /// <see cref="string"/>
        /// </summary>
        public static readonly Type String = typeof(string);

        /// <summary>
        /// <see cref="System.Guid"/>
        /// </summary>
        public static readonly Type Guid = typeof(Guid);

        /// <summary>
        /// <see cref="System.Version"/>
        /// </summary>
        public static readonly Type Version = typeof(Version);

        /// <summary>
        /// <see cref="System.DateTime"/>
        /// </summary>
        public static readonly Type DateTime = typeof(DateTime);

        /// <summary>
        /// <see cref="System.DateTimeOffset"/>
        /// </summary>
        public static readonly Type DateTimeOffset = typeof(DateTimeOffset);

        /// <summary>
        /// <see cref="System.Collections.IEnumerable"/>
        /// </summary>
        public static readonly Type IEnumerable = typeof(IEnumerable);

        /// <summary>
        /// <see cref="object"/>
        /// </summary>
        public static readonly Type Object = typeof(object);
    }
}

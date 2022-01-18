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
        /// <see cref="object"/>
        /// </summary>
        public static readonly Type Object = typeof(object);
    }
}

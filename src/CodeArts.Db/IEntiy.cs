using System;

namespace CodeArts.Db
{
    /// <summary>
    /// 实体基本接口。
    /// </summary>
    public interface IEntiy
    {
    }

    /// <summary>
    /// 实体基本接口。
    /// </summary>
    public interface IEntiy<TKey> : IEntiy where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        /// <summary>
        /// 主键。
        /// </summary>
        TKey Id { get; set; }
    }
}

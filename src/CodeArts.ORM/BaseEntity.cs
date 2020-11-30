using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace CodeArts.ORM
{
    /// <summary>
    /// 实体基类。
    /// </summary>
    public class BaseEntity : IEntiy
    {
    }
    /// <summary>
    /// 实体基类。
    /// </summary>
    /// <typeparam name="T">主键类型。</typeparam>
    [DebuggerDisplay("{Id}")]
    public class BaseEntity<T> : BaseEntity where T : IEquatable<T>, IComparable<T>
    {
        /// <summary>
        /// 主键。
        /// </summary>
        [Key]
        public virtual T Id { set; get; }

        /// <summary>
        /// 是否相等。
        /// </summary>
        /// <param name="obj">对象。</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if(obj is T value)
            {
                return value.Equals(Id);
            }

            if(obj is BaseEntity<T> entity)
            {
                return entity.Id.Equals(Id);
            }

            return false;
        }

        /// <summary>
        /// 获取哈希码。
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// 对象比较。
        /// </summary>
        /// <param name="left">对象1。</param>
        /// <param name="right">对象2。</param>
        /// <returns></returns>
        public static bool operator ==(BaseEntity<T> left, BaseEntity<T> right) => left.Id.Equals(right.Id);

        /// <summary>
        /// 对象比较。
        /// </summary>
        /// <param name="left">对象1。</param>
        /// <param name="right">对象2。</param>
        /// <returns></returns>
        public static bool operator !=(BaseEntity<T> left, BaseEntity<T> right) => !left.Id.Equals(right.Id);
    }
}

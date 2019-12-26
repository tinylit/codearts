using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace CodeArts.ORM
{
    /// <summary>
    /// 实体基类
    /// </summary>
    public class BaseEntity : IEntiy
    {
    }
    /// <summary>
    /// 实体基类
    /// </summary>
    /// <typeparam name="T">主键类型</typeparam>
    [DebuggerDisplay("{Id}")]
    public class BaseEntity<T> : BaseEntity
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        public virtual T Id { set; get; }
    }
}

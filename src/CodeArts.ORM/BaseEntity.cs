using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
    public class BaseEntity<T> : BaseEntity
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        public virtual T Id { set; get; }
    }
}

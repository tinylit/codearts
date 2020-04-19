using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 或者。
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class OrAttribute : ObjectiveAttribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public OrAttribute(ConditionType conditionType)
        {
            ConditionType = conditionType;
        }

        /// <summary>
        /// 关系
        /// </summary>
        public ConditionType ConditionType { get; }
    }
}

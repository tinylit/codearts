using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 并且。
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class AndAttribute : ObjectiveAttribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public AndAttribute(ConditionType conditionType)
        {
            ConditionType = conditionType;
        }

        /// <summary>
        /// 关系
        /// </summary>
        public ConditionType ConditionType { get; }
    }
}

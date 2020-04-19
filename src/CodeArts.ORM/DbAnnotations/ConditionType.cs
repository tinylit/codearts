namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 关系
    /// </summary>
    public enum ConditionType
    {
        /// <summary>
        /// 大于
        /// </summary>
        GreaterThan,
        /// <summary>
        /// 大于等于
        /// </summary>
        GreaterThanOrEqual,
        /// <summary>
        /// 相等
        /// </summary>
        Equal,
        /// <summary>
        /// 小于等于
        /// </summary>
        LessThanOrEqual,
        /// <summary>
        /// 小于
        /// </summary>
        LessThan,
        /// <summary>
        /// 不相等
        /// </summary>
        NotEqual,
        /// <summary>
        /// 以...开始
        /// </summary>
        StartWith,
        /// <summary>
        /// 包含
        /// </summary>
        Contains,
        /// <summary>
        /// 以...结束
        /// </summary>
        EndsWith
    }
}

namespace CodeArts
{
    /// <summary>
    /// 匹配性质。
    /// </summary>
    public enum PatternKind
    {
        /// <summary>
        /// 属性。
        /// </summary>
        Property = 1 << 0,
        /// <summary>
        /// 字段。
        /// </summary>
        Field = 1 << 1,
        /// <summary>
        /// 属性与字段。
        /// </summary>
        All = 1 << 2
    }
}

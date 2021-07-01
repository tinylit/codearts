namespace CodeArts.Db
{
    /// <summary>
    /// 查询访问器。
    /// </summary>
    public interface IQueryVisitor : IStartupVisitor
    {
        /// <summary>
        /// 是否必须有查询或执行结果。
        /// </summary>
        bool Required { get; }

        /// <summary>
        /// 包含默认值。
        /// </summary>
        bool HasDefaultValue { get; }

        /// <summary>
        /// 默认值。
        /// </summary>
        object DefaultValue { get; }

        /// <summary>
        /// 未查询到数据异常。
        /// </summary>
        string MissingDataError { get; }

        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间<see cref="System.Data.IDbCommand.CommandTimeout"/>。
        /// </summary>
        int? TimeOut { get; }
    }
}

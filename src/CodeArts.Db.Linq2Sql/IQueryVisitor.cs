namespace CodeArts.Db
{
    /// <summary>
    /// 查询访问器。
    /// </summary>
    public interface IQueryVisitor : IStartupVisitor
    {
        /// <summary>
        /// SQL语句。
        /// </summary>
        /// <returns></returns>
        CommandSql<T> ToSQL<T>();
    }
}

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 命令工厂。
    /// </summary>
    public interface IDbCommandFactory
    {
        /// <summary>
        /// 创建指令。
        /// </summary>
        /// <returns></returns>
        DbCommand CreateCommand();
    }
}

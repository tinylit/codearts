namespace CodeArts.ORM
{
    /// <summary>
    /// SQL写入器。
    /// </summary>
    public interface IWriterMap
    {
        /// <summary>
        /// 逗号。
        /// </summary>
        string Delimiter { get; }

        /// <summary>
        /// 左括号。
        /// </summary>
        string OpenBrace { get; }

        /// <summary>
        /// 右括号。
        /// </summary>
        string CloseBrace { get; }

        /// <summary>
        /// 空字符。
        /// </summary>
        string EmptyString { get; }

        /// <summary>
        /// 空格。
        /// </summary>
        string WhiteSpace { get; }

        /// <summary>
        /// 参数名称。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        string ParamterName(string name);

        /// <summary>
        /// 写入字段名。
        /// </summary>
        /// <param name="name">名称。</param>
        string Name(string name);
    }
}

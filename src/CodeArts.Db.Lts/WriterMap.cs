using System;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// SQL写入流。
    /// </summary>
    public class WriterMap : IWriterMap
    {
        private readonly ISQLCorrectSettings settings;
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="settings">SQL 矫正配置。</param>
        public WriterMap(ISQLCorrectSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
        /// <summary>
        /// ,
        /// </summary>
        public string Delimiter => ",";
        /// <summary>
        /// (
        /// </summary>
        public string OpenBrace => "(";
        /// <summary>
        /// )
        /// </summary>
        public string CloseBrace => ")";
        /// <summary>
        /// ''
        /// </summary>
        public string EmptyString => "''";
        /// <summary>
        /// “ ”
        /// </summary>
        public string WhiteSpace => " ";

        /// <summary>
        /// 参数名称。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public virtual string ParamterName(string name) => settings.ParamterName(name);
        /// <summary>
        /// 字段。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public virtual string Name(string name) => settings.Name(name);
    }
}

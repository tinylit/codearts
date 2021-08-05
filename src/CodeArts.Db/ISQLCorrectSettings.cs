using System.Collections.Generic;

namespace CodeArts.Db
{
    /// <summary>
    /// SQL矫正设置。
    /// </summary>
    public interface ISQLCorrectSettings
    {
        /// <summary>
        /// 数据库引擎。
        /// </summary>
        DatabaseEngine Engine { get; }

        /// <summary>
        /// 字符串截取。
        /// </summary>
        string Substring { get; }

        /// <summary>
        /// 索引位置。
        /// </summary>
        string IndexOf { get; }

        /// <summary>
        /// 长度测量器。
        /// </summary>
        string Length { get; }

        /// <summary>
        /// 索引交换位置（默认：value.indexOf("x") => IndexOfMethod(value,"x")）。
        /// </summary>
        bool IndexOfSwapPlaces { get; }

        /// <summary>
        /// 格式化集合（用作“<see cref="SQL.ToString(ISQLCorrectSettings)"/>”矫正SQL语句使用）。
        /// </summary>
        ICollection<IFormatter> Formatters { get; }

        /// <summary>
        /// 字段名称。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        string Name(string name);

        /// <summary>
        /// 参数名称。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        string ParamterName(string name);

        /// <summary>
        /// SQL(分页)。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="take">获取“<paramref name="take"/>”条数据。</param>
        /// <param name="skip">跳过“<paramref name="skip"/>”条数据。</param>
        /// <param name="orderBy">排序。</param>
        /// <returns></returns>
        string ToSQL(string sql, int take, int skip, string orderBy);
    }
}

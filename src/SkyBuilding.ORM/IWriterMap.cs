using System;
using System.Collections.Generic;
using System.Text;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// SQL写入器
    /// </summary>
    public interface IWriterMap
    {
        /// <summary>
        /// 逗号
        /// </summary>
        string Delimiter { get; }

        /// <summary>
        /// 左括号
        /// </summary>
        string OpenBrace { get; }

        /// <summary>
        /// 右括号
        /// </summary>
        string CloseBrace { get; }

        /// <summary>
        /// 空字符
        /// </summary>
        string EmptyString { get; }

        /// <summary>
        /// 空格
        /// </summary>
        string WhiteSpace { get; }
        /// <summary>
        /// 别名
        /// </summary>
        /// <param name="name">名称</param>
        string Alias(string name);

        /// <summary>
        /// 参数名称
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        string Paramter(string name);

        /// <summary>
        /// 写入字段名
        /// </summary>
        /// <param name="name">名称</param>
        string Name(string name);

        /// <summary>
        /// 写入表名
        /// </summary>
        /// <param name="name">名称</param>
        string TableName(string name);
    }
}

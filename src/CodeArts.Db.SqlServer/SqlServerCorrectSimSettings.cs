using CodeArts.Db.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeArts.Db.SqlServer
{
    /// <summary>
    /// SqlServer矫正设置。
    /// </summary>
    public class SqlServerCorrectSimSettings : ISQLCorrectSimSettings
    {
        /// <summary>
        /// 字符串截取。 SUBSTRING。
        /// </summary>
        public string Substring => "SUBSTRING";

        /// <summary>
        /// 字符串索引。 CHARINDEX。
        /// </summary>
        public string IndexOf => "CHARINDEX";

        /// <summary>
        /// 字符串长度。 LEN。
        /// </summary>
        public string Length => "LEN";

        /// <summary>
        /// 索引位置对调。 true。
        /// </summary>
        public bool IndexOfSwapPlaces => true;

        /// <summary>
        /// SqlServer。
        /// </summary>
        public DatabaseEngine Engine => DatabaseEngine.SqlServer;

        private ICollection<IFormatter> formatters;
        /// <summary>
        /// 格式化集合。
        /// </summary>
        public ICollection<IFormatter> Formatters => formatters ?? (formatters = new List<IFormatter>());

        /// <summary>
        /// 字段。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public string Name(string name) => string.Concat("[", name, "]");
        /// <summary>
        /// 参数名称。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public string ParamterName(string name) => string.Concat("@", name);
    }
}

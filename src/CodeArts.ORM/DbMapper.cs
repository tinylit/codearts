using System;
using System.Linq;
using System.Data;

namespace CodeArts.ORM
{
    /// <summary>
    /// 映射。
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class DbMapper<T>
    {
        /// <summary>
        /// 表实例。
        /// </summary>
        public static readonly ITableInfo TableInfo;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static DbMapper() => TableInfo = MapperRegions.Resolve(typeof(T));

        /// <summary>
        /// 验证名称是否有效。
        /// </summary>
        /// <param name="field">字段</param>
        /// <param name="isUpdateSet">是否是更新字段</param>
        /// <returns></returns>
        public static void EnsureValidField(string field, bool isUpdateSet)
        {
            if (!TableInfo.ReadOrWrites.Any(x => string.Equals(x.Value, field, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DataException($"表“{TableInfo.TableName}”不存在“{field}”字段!");
            }

            if (isUpdateSet && !TableInfo.ReadWrites.Any(x => string.Equals(x.Value, field, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DataException($"表“{TableInfo.TableName}”的“{field}”为只读字段!");
            }
        }
    }
}

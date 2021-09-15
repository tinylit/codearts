using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts.Db
{
    /// <summary>
    /// 函数名称列表。
    /// </summary>
    public class MethodCall
    {
        /// <summary>
        /// 查询。
        /// </summary>
        public const string Select = nameof(Queryable.Select);
        /// <summary>
        /// 查询。
        /// </summary>
        public const string SelectMany = nameof(Queryable.SelectMany);
        /// <summary>
        /// 求和。
        /// </summary>
        public const string Sum = nameof(Queryable.Sum);
        /// <summary>
        /// 最小值。
        /// </summary>
        public const string Min = nameof(Queryable.Min);
        /// <summary>
        /// 最大值。
        /// </summary>
        public const string Max = nameof(Queryable.Max);
        /// <summary>
        /// 总数(int)。
        /// </summary>
        public const string Count = nameof(Queryable.Count);
        /// <summary>
        /// 平均数。
        /// </summary>
        public const string Average = nameof(Queryable.Average);
        /// <summary>
        /// 总数(long)。
        /// </summary>
        public const string LongCount = nameof(Queryable.LongCount);
        /// <summary>
        /// 条件。
        /// </summary>
        public const string Where = nameof(Queryable.Where);
        /// <summary>
        /// 任意一个。
        /// </summary>
        public const string Any = nameof(Queryable.Any); //? IN 或 Exists
        /// <summary>
        /// 所有。
        /// </summary>
        public const string All = nameof(Queryable.All); //? Exists And Not Exists
        /// <summary>
        /// 转换。
        /// </summary>
        public const string Cast = nameof(Queryable.Cast); //? 在SQL中，只会生成共有的属性(不区分大小写)。
        /// <summary>
        /// 筛选类型属性。
        /// </summary>
        public const string OfType = nameof(Queryable.OfType); //? 在SQL中，只会生成共有的属性(不区分大小写)。
        /// <summary>
        /// 合并。
        /// </summary>
        public const string Join = nameof(Queryable.Join); //? INNER JOIN

        /// <summary>
        /// 合并。
        /// </summary>
        public const string GroupJoin = nameof(Queryable.GroupJoin); //? LEFT JOIN

        /// <summary>
        /// 第N个元素。
        /// </summary>
        public const string ElementAt = nameof(Queryable.ElementAt);
        /// <summary>
        /// 第N个元素，或默认值。
        /// </summary>
        public const string ElementAtOrDefault = nameof(Queryable.ElementAtOrDefault);
        /// <summary>
        /// 最后一个元素。
        /// </summary>
        public const string Last = nameof(Queryable.Last);
        /// <summary>
        /// 最后一个元素，或默认值。
        /// </summary>
        public const string LastOrDefault = nameof(Queryable.LastOrDefault);
        /// <summary>
        /// 第一个元素。
        /// </summary>
        public const string First = nameof(Queryable.First);
        /// <summary>
        /// 第一个元素，或默认值。
        /// </summary>
        public const string FirstOrDefault = nameof(Queryable.FirstOrDefault);
        /// <summary>
        /// 第一个元素。
        /// </summary>
        public const string Single = nameof(Queryable.Single);
        /// <summary>
        /// 第一个元素，或默认值。
        /// </summary>
        public const string SingleOrDefault = nameof(Queryable.SingleOrDefault);
        /// <summary>
        /// 获取N个元素。
        /// </summary>
        public const string Take = nameof(Queryable.Take);
        /// <summary>
        /// 从后往前获取N个元素。必须配合排序函数（OrderBy/OrderByDescending）使用。
        /// </summary>
        public const string TakeLast = "TakeLast"; //! 必须配合排序函数（OrderBy/OrderByDescending）使用。
        /// <summary>
        /// 获取条件。（与Where效果相同）
        /// </summary>
        public const string TakeWhile = nameof(Queryable.TakeWhile);
        /// <summary>
        /// 跳过N个元素。
        /// </summary>
        public const string Skip = nameof(Queryable.Skip);
        /// <summary>
        /// 从后往前跳过N个元素。必须配合排序函数（OrderBy/OrderByDescending）使用。
        /// </summary>
        public const string SkipLast = "SkipLast"; //! 必须配合排序函数（OrderBy/OrderByDescending）使用。
        /// <summary>
        /// 跳过条件。（与Where取反效果相同）。
        /// </summary>
        public const string SkipWhile = nameof(Queryable.SkipWhile);
        /// <summary>
        /// 去重。
        /// </summary>
        public const string Distinct = nameof(Queryable.Distinct); //? DESTINCT
        /// <summary>
        /// 分组。
        /// </summary>
        public const string GroupBy = nameof(Queryable.GroupBy); //? GROUP BY {AnyFiled}
        /// <summary>
        /// 正序。
        /// </summary>
        public const string OrderBy = nameof(Queryable.OrderBy); //? ORDER BY {AnyFiled}
        /// <summary>
        /// 正序。
        /// </summary>
        public const string ThenBy = nameof(Queryable.ThenBy); //? ORDER BY {AnyFiled}
        /// <summary>
        /// 倒序。
        /// </summary>
        public const string OrderByDescending = nameof(Queryable.OrderByDescending); //? ORDER BY {AnyFiled} DESC
        /// <summary>
        /// 倒序。
        /// </summary>
        public const string ThenByDescending = nameof(Queryable.ThenByDescending); //? ORDER BY {AnyFiled} DESC
        /// <summary>
        /// 设置为空时的默认值。
        /// </summary>
        public const string DefaultIfEmpty = nameof(Queryable.DefaultIfEmpty);
        /// <summary>
        /// 逆序。必须配合排序函数（OrderBy/OrderByDescending）使用。
        /// </summary>
        public const string Reverse = nameof(Queryable.Reverse); //! 必须配合排序函数（OrderBy/OrderByDescending）使用。
        /// <summary>
        /// 合并。 => UNION ALL。
        /// </summary>
        public const string Concat = nameof(Queryable.Concat); //? UNION ALL
        /// <summary>
        /// 并集。 => UNION。
        /// </summary>
        public const string Union = nameof(Queryable.Union); //? UNION
        /// <summary>
        /// 交集。 => INTERSECT。
        /// </summary>
        public const string Intersect = nameof(Queryable.Intersect); //? INTERSECT
        /// <summary>
        /// 排它 => EXCEPT。
        /// </summary>
        public const string Except = nameof(Queryable.Except); //? EXCEPT

        /** <see cref="string.IsNullOrEmpty(string)"/> */
        public const string IsNullOrEmpty = nameof(string.IsNullOrEmpty);

        /** <see cref="string.Replace(string, string)"/> */
        public const string Replace = nameof(string.Replace);

        /** <see cref="string.Substring(int)"/>/<seealso cref="string.Substring(int, int)"/> */
        public const string Substring = nameof(string.Substring);

        /// <summary>
        /// <see cref="string.IndexOf(string)"/>
        /// |<see cref="string.IndexOf(string, int)"/>
        /// |<seealso cref="string.IndexOf(string, int, int)"/>
        /// |<seealso cref="string.IndexOf(char)"/>
        /// |<seealso cref="string.IndexOf(char, int)"/>
        /// |<seealso cref="string.IndexOf(char, int, int)"/>
        /// </summary>
        public const string IndexOf = nameof(string.IndexOf);

        /// <summary>
        /// <see cref="string.ToUpper()"/>
        /// </summary>
        public const string ToUpper = nameof(string.ToUpper);

        /// <summary>
        /// <see cref="string.ToLower()"/>
        /// </summary>
        public const string ToLower = nameof(string.ToLower);

        /// <summary>
        /// <see cref="string.Trim()"/>
        /// </summary>
        public const string Trim = nameof(string.Trim);
        
        /// <summary>
        /// <see cref="string.TrimStart(char[])"/>
        /// </summary>
        public const string TrimStart = nameof(string.TrimStart);

        /// <summary>
        /// <see cref="string.TrimStart(char[])"/>
        /// </summary>
        public const string TrimEnd = nameof(string.TrimEnd);

        /// <summary>
        /// <see cref="string.EndsWith(string)"/>
        /// 以...结束。Like '%{AnyString}'。
        /// </summary>
        public const string EndsWith = nameof(string.EndsWith); //? Like '%{AnyString}'

        /// <summary>
        /// <see cref="string.StartsWith(string)"/>
        /// 以...开始。Like '{AnyString}%'。
        /// </summary>
        public const string StartsWith = nameof(string.StartsWith); //? Like '{AnyString}%'

        /// <summary>
        /// <see cref="string.Contains(string)"/>
        /// 包含。 Like '%{AnyString}%'。
        /// </summary>
        public const string Contains = nameof(string.Contains); //? Like '%{AnyString}%'

        /// <summary>
        /// <see cref="RepositoryExtentions.From{TSource}(IQueryable{TSource}, Func{ITableInfo, string})"/>
        /// </summary>
        public const string From = nameof(RepositoryExtentions.From);// "From";

        /// <summary>
        /// <see cref="RepositoryExtentions.TimeOut{TSource}(IQueryable{TSource}, int)"/>
        /// </summary>
        public const string TimeOut = nameof(RepositoryExtentions.TimeOut);// "TimeOut"

        /// <summary>
        /// <see cref="RepositoryExtentions.NoResultError{TSource}(IQueryable{TSource}, string)"/>
        /// 未查询到数据的异常消息(仅对最终的结果类型为【<see cref="string"/>】或【未继承<seealso cref="IEnumerable{T}"/>】的数据时，有效)。
        /// </summary>
        public const string NoResultError = nameof(RepositoryExtentions.NoResultError);// "NoResultError"

        /// <summary>
        /// 更新。
        /// </summary>
        public const string Update = nameof(RepositoryExtentions.Update); //"Update";

        /// <summary>
        /// 删除。
        /// </summary>
        public const string Delete = nameof(RepositoryExtentions.Delete); // "Delete";

        /// <summary>
        /// 插入。
        /// </summary>
        public const string Insert = nameof(RepositoryExtentions.Insert); // "Insert";

        /// <summary>
        /// 监视SQL。
        /// </summary>
        public const string WatchSql = nameof(RepositoryExtentions.WatchSql); // "WatchSql";
    }
}

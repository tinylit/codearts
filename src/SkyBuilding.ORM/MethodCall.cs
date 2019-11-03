using System.Linq;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 函数名称列表（“仅在Join状态下，且只有OrderBy可以使用非IEntity的参数类型。”）
    /// </summary>
    public class MethodCall
    {
        /// <summary>
        /// 查询
        /// </summary>
        public const string Select = "Select";

        /// <summary>
        /// 求和
        /// </summary>
        public const string Sum = "Sum";
        /// <summary>
        /// 最小值
        /// </summary>
        public const string Min = "Min";
        /// <summary>
        /// 最大值
        /// </summary>
        public const string Max = "Max";
        /// <summary>
        /// 总数(int)
        /// </summary>
        public const string Count = "Count";
        /// <summary>
        /// 平均数
        /// </summary>
        public const string Average = "Average";
        /// <summary>
        /// 总数(long)
        /// </summary>
        public const string LongCount = "LongCount";

        /// <summary>
        /// 条件
        /// </summary>
        public const string Where = "Where";
        /// <summary>
        /// 以...结束。Like '{AnyString}%'
        /// </summary>
        public const string EndsWith = "EndsWith"; //? Like '{AnyString}%'
        /// <summary>
        /// 以...开始。Like '%{AnyString}'
        /// </summary>
        public const string StartsWith = "StartsWith"; //? Like '%{AnyString}'
        /// <summary>
        /// 包含。 Like '%{AnyString}%'
        /// </summary>
        public const string Contains = "Contains"; //? Like '%{AnyString}%'

        /// <summary>
        /// 任意一个
        /// </summary>
        public const string Any = "Any"; //? IN 或 Exists
        /// <summary>
        /// 所有
        /// </summary>
        public const string All = "All"; //? Exists And Not Exists

        /// <summary>
        /// 转换
        /// </summary>
        public const string Cast = "Cast"; //? 在SQL中，只会生成共有的属性(不区分大小写)。

        /// <summary>
        /// 合并
        /// </summary>
        public const string Join = "Join"; //? LEFT JOIN

        /// <summary>
        /// 第N个元素
        /// </summary>
        public const string ElementAt = "ElementAt";
        /// <summary>
        /// 第N个元素，或默认值。
        /// </summary>
        public const string ElementAtOrDefault = "ElementAtOrDefault";

        /// <summary>
        /// 最后一个元素。
        /// </summary>
        public const string Last = "Last";
        /// <summary>
        /// 最后一个元素，或默认值。
        /// </summary>
        public const string LastOrDefault = "LastOrDefault";

        /// <summary>
        /// 第一个元素。
        /// </summary>
        public const string First = "First";
        /// <summary>
        /// 第一个元素，或默认值。
        /// </summary>
        public const string FirstOrDefault = "FirstOrDefault";

        /// <summary>
        /// 第一个元素。
        /// </summary>
        public const string Single = "Single";
        /// <summary>
        /// 第一个元素，或默认值。
        /// </summary>
        public const string SingleOrDefault = "SingleOrDefault";

        /// <summary>
        /// 获取N个元素。
        /// </summary>
        public const string Take = "Take";
        /// <summary>
        /// 从后往前获取N个元素。必须配合排序函数（OrderBy/OrderByDescending）使用。
        /// </summary>
        public const string TakeLast = "TakeLast"; //! 必须配合排序函数（OrderBy/OrderByDescending）使用。
        /// <summary>
        /// 获取条件。（与Where效果相同）
        /// </summary>
        public const string TakeWhile = "TakeWhile";

        /// <summary>
        /// 跳过N个元素。SqlServer中，必须配合排序函数（OrderBy/OrderByDescending）使用。
        /// </summary>
        public const string Skip = "Skip"; //! SqlServer中，必须配合排序函数（OrderBy/OrderByDescending）使用。
        /// <summary>
        /// 从后往前跳过N个元素。必须配合排序函数（OrderBy/OrderByDescending）使用。
        /// </summary>
        public const string SkipLast = "SkipLast"; //! 必须配合排序函数（OrderBy/OrderByDescending）使用。
        /// <summary>
        /// 跳过条件。（与Where取反效果相同）
        /// </summary>
        public const string SkipWhile = "SkipWhile";

        /// <summary>
        /// 去重
        /// </summary>
        public const string Distinct = "Distinct"; //? DESTINCT
        /// <summary>
        /// 正序
        /// </summary>
        public const string OrderBy = "OrderBy"; //? ORDER BY {AnyFiled}
        /// <summary>
        /// 正序
        /// </summary>
        public const string ThenBy = "ThenBy"; //? ORDER BY {AnyFiled}
        /// <summary>
        /// 倒序
        /// </summary>
        public const string OrderByDescending = "OrderByDescending"; //? ORDER BY {AnyFiled} DESC
        /// <summary>
        /// 倒序
        /// </summary>
        public const string ThenByDescending = "ThenByDescending"; //? ORDER BY {AnyFiled} DESC

        /// <summary>
        /// 设置为空时的默认值。
        /// </summary>
        public const string DefaultIfEmpty = "DefaultIfEmpty";

        /// <summary>
        /// 逆序。必须配合排序函数（OrderBy/OrderByDescending）使用。
        /// </summary>
        public const string Reverse = "Reverse"; //! 必须配合排序函数（OrderBy/OrderByDescending）使用。

        /// <summary>
        /// 合并。 => UNION ALL
        /// </summary>
        public const string Concat = "Concat"; //? UNION ALL

        /// <summary>
        /// 并集。 => UNION
        /// </summary>
        public const string Union = "Union"; //? UNION

        /// <summary>
        /// 交集。 => INTERSECT
        /// </summary>
        public const string Intersect = "Intersect"; //? INTERSECT


        /** 以下为 string 扩展 */
        public const string IsNullOrEmpty = "IsNullOrEmpty";
        /** 以下为 string 扩展 */
        public const string Replace = "Replace";
        /** 以下为 string 扩展 */
        public const string Substring = "Substring";
        /** 以下为 string 扩展 */
        public const string IndexOf = "IndexOf";
        /** 以下为 string 扩展 */
        public const string ToUpper = "ToUpper";
        /** 以下为 string 扩展 */
        public const string ToLower = "ToLower";
        /** 以下为 string 扩展 */
        public const string Trim = "Trim";
        /** 以下为 string 扩展 */
        public const string TrimStart = "TrimStart";
        /** 以下为 string 扩展 */
        public const string TrimEnd = "TrimEnd";

        /** 查询器扩展 */
        public const string From = nameof(QueryableStrengthen.From);//"From";

        /// <summary>
        /// 获取第一个元素。
        /// </summary>
        public const string TakeFirst = nameof(QueryableStrengthen.TakeFirst);// "TakeFirst";
        /// <summary>
        /// 获取第一个元素，或默认值。
        /// </summary>
        public const string TakeFirstOrDefault = nameof(QueryableStrengthen.TakeFirstOrDefault);// "TakeFirstOrDefault";
        /// <summary>
        /// 获取第一个元素。
        /// </summary>
        public const string TakeSingle = nameof(QueryableStrengthen.TakeSingle);// "TakeSingle";
        /// <summary>
        /// 获取第一个元素，或默认值。
        /// </summary>
        public const string TakeSingleOrDefault = nameof(QueryableStrengthen.TakeSingleOrDefault);//"TakeSingleOrDefault";
        /// <summary>
        /// 获取最后一个元素，或默认值。
        /// </summary>
        public const string TakeLastOrDefault = nameof(QueryableStrengthen.TakeLastOrDefault);// "TakeLastOrDefault";

        /// <summary>
        /// 更新
        /// </summary>
        public const string Update = nameof(Executeable.Update); //"Update";
        /// <summary>
        /// 删除
        /// </summary>
        public const string Delete = nameof(Executeable.Delete); // "Delete";
        /// <summary>
        /// 插入
        /// </summary>
        public const string Insert = nameof(Executeable.Insert); // "Insert";

        /** 以下不支持 */

        internal const string Except = "Except";
    }
}

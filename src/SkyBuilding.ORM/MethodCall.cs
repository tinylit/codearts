using System.Linq;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 函数名称列表（“仅在Join状态下，且只有OrderBy可以使用非IEntity的参数类型。”）
    /// </summary>
    public class MethodCall
    {
        public const string Select = "Select";

        public const string Sum = "Sum";
        public const string Min = "Min";
        public const string Max = "Max";
        public const string Count = "Count";
        public const string Average = "Average";
        public const string LongCount = "LongCount";

        public const string Where = "Where";
        public const string EndsWith = "EndsWith"; //? Like '{AnyString}%'
        public const string StartsWith = "StartsWith"; //? Like '%{AnyString}'
        public const string Contains = "Contains"; //? Like '%{AnyString}%'

        public const string Any = "Any"; //? IN 或 Exists
        public const string All = "All"; //? Exists And Not Exists

        public const string Cast = "Cast"; //? 在SQL中，只会生成共有的属性(不区分大小写)。

        public const string Join = "Join"; //? LEFT JOIN

        public const string ElementAt = "ElementAt";
        public const string ElementAtOrDefault = "ElementAtOrDefault";

        public const string Last = "Last";
        public const string LastOrDefault = "LastOrDefault";

        public const string First = "First";
        public const string FirstOrDefault = "FirstOrDefault";

        public const string Single = "Single";
        public const string SingleOrDefault = "SingleOrDefault";

        public const string Take = "Take";
        public const string TakeLast = "TakeLast"; //! 必须配合排序函数（OrderBy/OrderByDescending）使用。
        public const string TakeWhile = "TakeWhile";

        public const string Skip = "Skip"; //! SqlServer中，必须配合排序函数（OrderBy/OrderByDescending）使用。
        public const string SkipLast = "SkipLast"; //! 必须配合排序函数（OrderBy/OrderByDescending）使用。
        public const string SkipWhile = "SkipWhile";


        public const string Distinct = "Distinct"; //? DESTINCT
        public const string OrderBy = "OrderBy"; //? ORDER BY {AnyFiled}
        public const string ThenBy = "ThenBy"; //? ORDER BY {AnyFiled}
        public const string OrderByDescending = "OrderByDescending"; //? ORDER BY {AnyFiled} DESC
        public const string ThenByDescending = "ThenByDescending"; //? ORDER BY {AnyFiled} DESC

        public const string Reverse = "Reverse"; //! 必须配合排序函数（OrderBy/OrderByDescending）使用。

        public const string Concat = "Concat"; //? UNION ALL

        public const string Union = "Union"; //? UNION

        public const string Intersect = "Intersect"; //? INTERSECT

        /** 以下为 string 扩展 */
        public const string IsNullOrEmpty = "IsNullOrEmpty";
        public const string Replace = "Replace";
        public const string Substring = "Substring";
        public const string IndexOf = "IndexOf";
        public const string ToUpper = "ToUpper";
        public const string ToLower = "ToLower";
        public const string Trim = "Trim";
        public const string TrimStart = "TrimStart";
        public const string TrimEnd = "TrimEnd";

        /** 查询器扩展 */
        public const string From = nameof(QueryableStrengthen.From);//"From";

        public const string TakeFirst = nameof(QueryableStrengthen.TakeFirst);// "TakeFirst";
        public const string TakeFirstOrDefault = nameof(QueryableStrengthen.TakeFirstOrDefault);// "TakeFirstOrDefault";
        public const string TakeSingle = nameof(QueryableStrengthen.TakeSingle);// "TakeSingle";
        public const string TakeSingleOrDefault = nameof(QueryableStrengthen.TakeSingleOrDefault);//"TakeSingleOrDefault";
        public const string TakeLastOrDefault = nameof(QueryableStrengthen.TakeLastOrDefault);// "TakeLastOrDefault";

        /** 执行器扩展 */
        public const string Update = nameof(Executeable.Update); //"Update";
        public const string Delete = nameof(Executeable.Delete); // "Delete";
        public const string Insert = nameof(Executeable.Insert); // "Insert";

        /** 以下不支持 */

        internal const string Except = "Except";
    }
}

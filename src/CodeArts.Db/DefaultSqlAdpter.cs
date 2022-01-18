using CodeArts.Db.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeArts.Db
{
    /// <summary>
    /// 默认SQL解析器。
    /// </summary>
    public class DefaultSqlAdpter : ISqlAdpter
    {
        const char LeftBracket = '[';
        const char RightBracket = ']';
        /// <summary>
        /// 注解。
        /// </summary>
        private static readonly Regex PatternAnnotate = new Regex(@"--.*|/\*[\s\S]*?\*/", RegexOptions.Compiled);

        /// <summary>
        /// 提取字符串。
        /// </summary>
        private static readonly Regex PatternCharacter = new Regex("(['\"])(?:\\\\.|[^\\\\])*?\\1", RegexOptions.Compiled);

        /// <summary>
        /// 空白符。
        /// </summary>
        private static readonly Regex PatternWhitespace = new Regex(@"(?<=[\x20\t\f])[\x20\t\f]+|(?<=\r\n)[\x20\t\r\n\f]*[\r\n\f]+|(?<=\()[\x20\t\r\n\f]+|[\x20\t\r\n\f]+(?=\))|[\x20\t\r\n\f]+$", RegexOptions.Compiled);

        /// <summary>
        /// 移除所有前导空白字符和尾部空白字符函数修复。
        /// </summary>
        private static readonly Regex PatternTrim = new Regex(@"\b(?<name>(trim))\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 修改命令。
        /// </summary>
        private static readonly Regex PatternAlter = new Regex(@"\balter[\x20\t\r\n\f]+(table|view)[\x20\t\r\n\f]+((\w+|\[\w+\])\.)*(?<table>(?<name>\w+)|\[(?<name>\w+)\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 插入命令。
        /// </summary>
        private static readonly Regex PatternInsert = new Regex(@"\binsert[\x20\t\r\n\f]+into[\x20\t\r\n\f]+((\w+|\[\w+\])\.)*(?<table>(?<name>\w+)|\[(?<name>\w+)\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 简单删除命令。
        /// </summary>
        private static readonly Regex PatternDelete = new Regex(@"\bdelete[\x20\t\r\n\f]+from[\x20\t\r\n\f]+((\w+|\[\w+\])\.)*(?<table>(?<name>\w+)|\[(?<name>\w+)\])(?<nickname>[\x20\t\r\n\f]+(\[\w+\]|as[\x20\t\r\n\f]+(?<alias>\w+)|as[\x20\t\r\n\f]+\[\w+\]|(?<alias>(?!\b(where|on|join|group|order|select|into|limit|(left|right|inner|outer)[\x20\t\r\n\f]+join)\b)\w+)))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 连表删除命令。
        /// </summary>
        private static readonly Regex PatternDeleteMulti = new Regex(@"\bdelete[\x20\t\r\n\f]+((\w+|\[\w+\])\.)*(?<table>(?<name>\w+)|\[(?<name>\w+)\])(?<nickname>[\x20\t\r\n\f]+(\[\w+\]|as[\x20\t\r\n\f]+(?<alias>\w+)|as[\x20\t\r\n\f]+\[\w+\]|(?<alias>(?!\bfrom\b)\w+)))?((?!\bfrom\b)[\s\S]+?)?(?=[\x20\t\r\n\f]+from[\x20\t\r\n\f]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 更新指令。
        /// </summary>

        private static readonly Regex PatternUpdate = new Regex(@"\bupdate[\x20\t\r\n\f]+((\w+|\[\w+\])\.)*(?<table>(?<name>\w+)|\[(?<name>\w+)\])(?<nickname>[\x20\t\r\n\f]+(\[\w+\]|as[\x20\t\r\n\f]+(?<alias>\w+)|as[\x20\t\r\n\f]+\[\w+\]|(?<alias>(?!\bset\b)\w+)))?((?!\bset\b)[\s\S]+?)?[\x20\t\r\n\f]+set[\x20\t\r\n\f]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 创建表命令。
        /// </summary>
        private static readonly Regex PatternCreate = new Regex(@"\bcreate[\x20\t\r\n\f]+(table|view)[\x20\t\r\n\f]+(?<if>if[\x20\t\r\n\f]+not[\x20\t\r\n\f]+exists[\x20\t\r\n\f]+)?((\w+|\[\w+\])\.)*(?<table>(?<name>\w+)|\[(?<name>\w+)\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 删除表命令。
        /// </summary>
        private static readonly Regex PatternDrop = new Regex(@"\bdrop[\x20\t\r\n\f]+(table|view)[\x20\t\r\n\f]+(?<if>if[\x20\t\r\n\f]+exists[\x20\t\r\n\f]+)?([\w\[\]]+\.)*(?<table>(?<name>\w+)|\[(?<name>\w+)\])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 表命令。
        /// </summary>
        private static readonly Regex PatternForm = new Regex(@"\bfrom[\x20\t\r\n\f]+((\w+|\[\w+\])\.)*(?<table>(?<name>\w+)|\[(?<name>\w+)\])([\x20\t\r\n\f]+(\[\w+\]|as[\x20\t\r\n\f]+(?<alias>\w+)|as[\x20\t\r\n\f]+\[\w+\]|(?<alias>(?!\b(where|on|join|group|order|select|into|limit|(left|right|inner|outer)[\x20\t\r\n\f]+join)\b)\w+)))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 参数。
        /// </summary>
        private static readonly Regex PatternParameter = new Regex(@"(?<![\p{L}\p{N}@_])[?:@](?<name>[\p{L}\p{N}_][\p{L}\p{N}@_]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// 字段名称。
        /// </summary>
        private static readonly Regex PatternField;

        /// <summary>
        /// 字段名称补充。
        /// </summary>
        private static readonly Regex PatternFieldEmbody = new Regex(@"\[\w+\][\x20\t\r\n\f]*,[\x20\t\r\n\f]*(?<name>[_a-zA-Z]\w*)(?=[\x20\t\r\n\f]+[^\x20\t\r\n\f\(]|[^\x20\t\r\n\f\w\.\]\}\(]|[\x20\t\r\n\f]*$)", RegexOptions.Compiled);

        /// <summary>
        /// 别名字段。
        /// </summary>
        private static readonly Regex PatternAliasField = new Regex(@"(?<alias>[\w-]+)\.(?<name>(?!\d+)[\w-]+)(?=[^\w\.\]\}\(]|$)", RegexOptions.Compiled);

        /// <summary>
        /// 独立参数字段。
        /// </summary>
        private static readonly Regex PatternSingleArgField = new Regex(@"(?<!with[\x20\t\r\n\f]*)\([\x20\t\r\n\f]*(?<name>(?!\d+)(?![_A-Z]+)(?!\b(max|min)\b)\w+)[\x20\t\r\n\f]*\)", RegexOptions.Compiled);

        /// <summary>
        /// 字段名称（创建别命令中）。
        /// </summary>
        private static readonly Regex PatternFieldCreate;

        /// <summary>
        /// 字段别名。
        /// </summary>
        private static readonly Regex PatternAsField = new Regex(@"\[\w+\][\x20\t\r\n\f]+as[\x20\t\r\n\f]+(?<name>[\p{L}\p{N}@_]+)|\bas[\x20\t\r\n\f]+(?<name>(?!\bselect\b)[\p{L}\p{N}@_]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// 查询语句所有字段。
        /// </summary>
        private static readonly Regex PatternColumn = new Regex(@"\bselect[\x20\t\r\n\f]+(?<distinct>distinct[\x20\t\r\n\f]+)?(?<cols>((?!\b(select|where)\b)[\s\S])+(select((?!\b(from|select)\b)[\s\S])+from((?!\b(from|select)\b)[\s\S])+)*((?!\b(from|select)\b)[\s\S])*)[\x20\t\r\n\f]+from[\x20\t\r\n\f]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 查询语句排序内容。
        /// </summary>
        private static readonly Regex PatternOrderBy = new Regex(@"[\x20\t\r\n\f]+order[\x20\t\r\n\f]+by[\x20\t\r\n\f]+((?!\b(select|where)\b)[\s\S])+(select((?!\b(from|select)\b)[\s\S])+from((?!\b(from|select)\b)[\s\S])+)*((?!\b(from|select)\b)[\s\S])*$", RegexOptions.IgnoreCase | RegexOptions.RightToLeft | RegexOptions.Compiled);

        /// <summary>
        /// 分页。
        /// </summary>
        private static readonly Regex PatternPaging = new Regex("P\\(`(?<main>(?:\\\\.|[^\\\\])*?)`,(?<index>\\d+),(?<size>\\d+)(,`(?<orderby>(?:\\\\.|[^\\\\])*?)`)?\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 查询别名。
        /// </summary>
        private static readonly Regex PatternWithAs = new Regex(@"\bwith[\x20\t\r\n\f]+(?<name>[^\x20\t\r\n\f]+)[\x20\t\r\n\f]+as[\x20\t\r\n\f]*\((?<sql>.+?)\)([\x20\t\r\n\f]*,[\x20\t\r\n\f]*(?<name>[^\x20\t\r\n\f]+)[\x20\t\r\n\f]+as[\x20\t\r\n\f]*\((?<sql>.+?)\))*[\x20\t\r\n\f]*(?=select|insert|update|delete[\x20\t\r\n\f]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

        #region UseSettings

        /// <summary>
        /// 信任的函数。
        /// </summary>
        private static readonly Regex PatternConvincedMethod = new Regex(@"\b(?<name>(len|length|substr|substring|indexof))(?=\()", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 字符串截取函数修复。
        /// </summary>
        private static readonly Regex PatternIndexOf = new Regex(@"\bindexOf\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 名称令牌。
        /// </summary>
        private static readonly Regex PatternNameToken = new Regex(@"\[(?<name>\w+)\]", RegexOptions.Compiled);

        /// <summary>
        /// 参数令牌。
        /// </summary>
        private static readonly Regex PatternParameterToken = new Regex(@"\{(?<name>[\p{L}\p{N}@_]+)\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// 聚合函数。
        /// </summary>
        private static readonly Regex PatternAggregationFn = new Regex(@"\b(sum|max|min|avg|count)[\x20\t\r\n\f]*\(", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// 字段名。
        /// </summary>
        private static readonly Regex PatternSingleAsColumn = new Regex(@"(?<=[\.\x20\t\r\n\f])(?<name>(\[\w+\]|((?!(end)))\w+))[\x20\t\r\n\f]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

        #endregion

        private static readonly ConcurrentDictionary<string, string> SqlCache = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> SqlCountCache = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> SqlPagedCache = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, List<string>> CharacterCache = new ConcurrentDictionary<string, List<string>>();
        private static readonly ConcurrentDictionary<string, IReadOnlyList<TableToken>> TableCache = new ConcurrentDictionary<string, IReadOnlyList<TableToken>>();

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static DefaultSqlAdpter()
        {
            var whitespace = @"[\x20\t\r\n\f]";
            var check = @"(?=[\x20\t\r\n\f]+[^\x20\t\r\n\f\(]|[^\x20\t\r\n\f\w\.\]\}\(]|[\x20\t\r\n\f]*$)";

            var sb = new StringBuilder()
                .Append(",")
                    .Append(whitespace)
                    .Append(@"*(?<name>(?!\d+)\w+)")
                    .Append(whitespace)
                    .Append(@"*(?=[=,])") //? ,[name], ,[name]=[value]
                .Append(@"|\]")
                    .Append(whitespace)
                    .Append(@"*(?<name>(?!\d+)\w+)")
                    .Append(whitespace)
                    .Append("*(?=,)")   //? [name] alias,
                .Append(@"|\bcolumn")
                    .Append(whitespace)
                    .Append(@"+(?<name>(?!\d+)\w+)") //? 字段 column [name]
                .Append(@"|\bafter")
                    .Append(whitespace)
                    .Append(@"+(?<name>(?!\d+)\w+)")
                    .Append(whitespace)
                    .Append("*(?=(;|$))") //? after [name] --mysql alter table `yep_developers` add column `level` int default 0 after `status`;
                .Append(@"|\bupdate")
                    .Append(whitespace)
                    .Append(@"+.+?")
                    .Append(whitespace)
                    .Append("+set")
                    .Append(whitespace)
                    .Append(@"+(?<name>(?!\d+)(?!\bwhen\b)\w+)")
                    .Append(check) //? 更新语句SET字段
                .Append(@"|\bcase").Append(whitespace)
                    .Append(@"+(?<name>(?!\d+)(?!\bwhen\b)\w+)")
                    .Append(check) //? case 函数
                .Append(@"|\bwhen")
                    .Append(whitespace)
                    .Append(@"+(")
                        .Append("(not")
                            .Append(whitespace)
                            .Append("+)?exists")
                            .Append(whitespace)
                            .Append(@"*\(")
                            .Append(whitespace)
                            .Append("*select")
                            .Append(whitespace)
                            .Append("+(distinct")
                            .Append(whitespace)
                            .Append("+)?")
                            .Append("(top")
                                .Append(whitespace)
                                .Append(@"+\d+")
                                .Append(whitespace)
                                .Append("+")
                            .Append(")?")
                        .Append(")?")
                    .Append(@"(?<name>(?!\d+)(?!\b(not|then|top|distinct)\b)\w+)")
                    .Append(check) //? when 函数
                .Append(@"|\b(then|else)")
                    .Append(whitespace)
                    .Append(@"+(?<name>(?!\d+)(?!\bnull\b)\w+)")
                    .Append(check) //? case [name] when [name] then [name] else [name] end;
                .Append(@"|\bby")
                    .Append(whitespace)
                    .Append(@"+(?<name>(?!\d+)\w+)")
                    .Append(check) //?  by [name];
                .Append(@"|\bselect")
                    .Append(whitespace)
                    .Append("+(distinct")
                    .Append(whitespace)
                    .Append("+)?")
                    .Append("(top")
                    .Append(whitespace)
                    .Append(@"+\d+")
                    .Append(whitespace)
                    .Append("+")
                    .Append(")?")
                    .Append(@"(?<name>(?!\d+)(?!\b(top|distinct)\b)\w+)")
                    .Append(check) //? select [name]; select top 10 [name]
                .Append(@"|\b(where|and|or|between)")
                    .Append(whitespace)
                    .Append(@"+(")
                        .Append("(not")
                            .Append(whitespace)
                            .Append("+)?exists")
                            .Append(whitespace)
                            .Append(@"*\(")
                            .Append(whitespace)
                            .Append("*select")
                            .Append(whitespace)
                            .Append("+(distinct")
                            .Append(whitespace)
                            .Append("+)?")
                            .Append("(top")
                                .Append(whitespace)
                                .Append(@"+\d+")
                                .Append(whitespace)
                                .Append("+")
                            .Append(")?")
                        .Append(")?")
                    .Append(@"(?<name>(?!\d+)(?!\b(not|top|distinct)\b)\w+)")
                    .Append(check) //? where not exists() where [name];
                .Append(@"|\bon")
                    .Append(whitespace)
                    .Append(@"+(?<name>(?!\d+)(?!\b(update|delete)\b)\w+)")
                    .Append(check) //? on [name]; -- mysql `modified` timestamp(0) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(0)
                .Append("|[<=%>+/-]")
                    .Append(whitespace)
                    .Append(@"*(?<name>(?!\d+)\w+)")
                    .Append(whitespace)
                    .Append(@"+(?=(and|or|case|when|then|else|end|between|select|by|join|on|(left|right|inner|outer)")
                    .Append(whitespace)
                    .Append(@"+join") // ? =[name] and
                    .Append(whitespace)
                    .Append("+))")
                .Append(@"|\*")
                    .Append(whitespace)
                    .Append(@"*(?<name>(?!\d+)(?!\bfrom\b)\w+)")
                    .Append(check) //? *[value]
                .Append(@"|(?<!convert[\x20\t\r\n\f]*)\(")
                    .Append(whitespace)
                    .Append(@"*(?<name>(?!\d+)\w+)")
                    .Append(whitespace)
                    .Append(@"*,") //? ([name],
                .Append(@"|,")
                    .Append(whitespace)
                    .Append(@"*(?<name>(?!\d+)\w+)")
                    .Append(whitespace)
                    .Append(@"*\)") //? ,[name])
                .Append(@"|\(")
                    .Append(whitespace)
                    .Append(@"*(?<name>(?!\d+)\w+)")
                    .Append(whitespace)
                    .Append(@"*(?=[<=%>+/-])") //? ([name]=
                .Append(@"|[<=%>+/-]")
                    .Append(whitespace)
                    .Append(@"*(?<name>(?!\d+)\w+)")
                    .Append(whitespace)
                    .Append(@"*\)") //? =[name])
                .Append("|[&|^]")
                    .Append(whitespace)
                    .Append(@"*(?<name>(?!\d+)\w+)")
                    .Append(whitespace)
                    .Append("*(?=[<=%>+/-])") //? &[name]= |[name]= -- 位运算
                .Append("|[<=%>+/-]")
                    .Append(whitespace)
                    .Append(@"*(?<name>(?!\d+)\w+)")
                    .Append(whitespace)
                    .Append("*(?=[&|^])"); //? +[name]& +[name]| -- 位运算

            PatternField = new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);

            sb = new StringBuilder();

            sb.Append("^")
                .Append(whitespace)
                .Append(@"*\(")
                .Append(whitespace)
                .Append(@"*(?<name>[_a-z]\w*)")
                .Append(check) //? ([name]
                    .Append("|(,")
                    .Append(whitespace)
                    .Append(@"*(?<name>(?!\b(key|primary|unique|foreign|constraint)\b)[_a-z]\w*)")
                    .Append(check)
                    .Append(")");

            PatternFieldCreate = new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        #region Private
        /// <summary>
        /// 分析参数。
        /// </summary>
        /// <param name="sql">内容。</param>
        /// <param name="startIndex">开始位置。</param>
        /// <param name="matches">匹配参数结果。</param>
        /// <returns></returns>
        private static int ParameterAnalysis(string sql, int startIndex, out List<RangeMatch> matches)
        {
            matches = new List<RangeMatch>();

            bool quotesFlag = false;

            int letterStart = 0;
            int bracketLeft = 0;
            int bracketRight = 0;

            //? 字段初始。
            int parameterStart = startIndex;

            for (int i = startIndex, length = sql.Length; i < length; i++) //? 空白符处理。
            {
                char c = sql[i];

                if (c == '\\') //? 转义。
                {
                    i++;

                    continue;
                }

                if (c == '\'')
                {
                    if (quotesFlag) //? 右引号。
                    {
                        quotesFlag = false;

                        continue;
                    }

                    quotesFlag = true;
                }

                if (quotesFlag) //? 占位符。
                {
                    continue;
                }

                if (c == '(')
                {
                    bracketLeft++;
                }
                else if (c == ')')
                {
                    if (bracketRight == bracketLeft)
                    {
                        letterStart = i;

                        break;
                    }

                    bracketRight++;
                }
                else if (bracketLeft == bracketRight && c == ',') //? 字段。
                {
                    matches.Add(new RangeMatch
                    {
                        Index = parameterStart,
                        Length = i - parameterStart
                    });

                    parameterStart = i + 1;
                }
            }

            matches.Add(new RangeMatch
            {
                Index = parameterStart,
                Length = letterStart - parameterStart
            });

            return letterStart;
        }

        /// <summary>
        /// 是空白符。
        /// </summary>
        /// <param name="c">符号。</param>
        /// <returns></returns>
        private static bool IsWhitespace(char c) => c == '\x20' || c == '\t' || c == '\r' || c == '\n' || c == '\f';

        /// <summary>
        /// 片段。
        /// </summary>
        /// <param name="c">符号。</param>
        /// <returns></returns>
        private static bool IsFragment(char c) => '_' == c || char.IsLetter(c) || char.IsNumber(c);

        private static bool TryDistinctField(string cols, out string col)
        {
            if (cols.Length == 0 || PatternAggregationFn.IsMatch(cols))
            {
                goto label_false;
            }

            int startIndex = -1;
            int i = 0, length = cols.Length;

label_core:

            for (; i < length; i++)
            {
                char c = cols[i];

                if (IsWhitespace(c))
                {
                    if (startIndex == -1)
                    {
                        startIndex = i;

                        continue;
                    }

                    bool quotesFlag = false;

                    for (int j = i + 1; j < length; j++)
                    {
                        c = cols[j];

                        if (c == '\'')
                        {
                            if (quotesFlag) //? 右引号。
                            {
                                quotesFlag = false;

                                continue;
                            }

                            quotesFlag = true;
                        }

                        if (quotesFlag) //? 占位符。
                        {
                            continue;
                        }

                        if (c == RightBracket || c == '`' || c == '"')//? 字段标识。
                        {
                            i = j + 1;

                            continue;
                        }

                        if (c == '+' || c == '-' || c == '*' || c == '/' || c == '%' || c == ',' || c == '(' || c == ')') //? 分隔符。
                        {
                            i = j + 1;

                            goto label_core;
                        }

                        if (IsWhitespace(c))
                        {
                            break;
                        }
                    }

                    break;
                }
                else if (startIndex == -1)
                {
                    startIndex = i;
                }
            }

            if (startIndex > -1 && LikeAs(cols, i))
            {
#if NETSTANDARD2_1_OR_GREATER
                col = cols[startIndex..i];
#else
                col = cols.Substring(startIndex, i - startIndex);
#endif

                return true;
            }

label_false:

            col = null;

            return false;
        }

        private static bool LikeAs(string cols, int startIndex)
        {
            bool firstAwordsFlag = false;
            bool nonfirstWordsFlag = true;
            int i = startIndex, length = cols.Length;

            for (; i < length; i++)
            {
                char c = cols[i];

                if (IsWhitespace(c))
                {
                    if (nonfirstWordsFlag) //? 首个字母。
                    {
                        continue;
                    }

                    return false;
                }
                else if (nonfirstWordsFlag)
                {
                    if (firstAwordsFlag)
                    {
                        firstAwordsFlag = false;

                        if (c == 's' || c == 'S')
                        {
                            continue;
                        }
                    }

                    if (c == 'a' || c == 'A')
                    {
                        firstAwordsFlag = true;

                        continue;
                    }

                    nonfirstWordsFlag = false;
                }
            }

            return true;
        }

        /// <summary>
        /// 独立SQL分析。
        /// </summary>
        /// <param name="sql">内容。</param>
        /// <param name="startIndex">开始位置。</param>
        /// <returns></returns>
        private static int IndependentSqlAnalysis(string sql, int startIndex)
        {
            int length = sql.Length;

            for (; startIndex < length; startIndex++) //? 空白符处理。
            {
                char c = sql[startIndex];

                if (IsWhitespace(c))
                {
                    continue;
                }

                break;
            }

            int indexOfCemicolon = sql.IndexOf(';', startIndex);

            if (indexOfCemicolon > -1) //? 分号分割。
            {
                length = indexOfCemicolon + 1;
            }

            bool flag = true;
            bool quotesFlag = false;

            int letterStart = 0;

            int bracketLeft = 0;
            int bracketRight = 0;

            for (int i = startIndex + 6/* 关键字 */; i < length; i++) //? 空白符处理。
            {
                char c = sql[i];

                if (c == '\'')
                {
                    if (quotesFlag) //? 右引号。
                    {
                        quotesFlag = false;

                        continue;
                    }

                    quotesFlag = true;
                }

                if (quotesFlag) //? 占位符。
                {
                    continue;
                }

                if (flag && IsFragment(c))
                {
                    flag = false;

                    letterStart = i;

                    continue;
                }

                if (c == '(')
                {
                    bracketLeft++;
                }
                else if (c == ')')
                {
                    bracketRight++;

                    goto label_check;
                }
                else if (IsWhitespace(c))
                {
                    flag = true;

                    goto label_check;
                }

                continue;

label_check:

                if (letterStart == 0 || bracketLeft != bracketRight)
                {
                    continue;
                }

                switch (i - letterStart)
                {
                    case 4 when IsWith(sql, letterStart):
                    case 6 when IsDelete(sql, letterStart)
                                || IsInsert(sql, letterStart)
                                || IsUpdate(sql, letterStart):
                        return letterStart - 1;
                    case 6 when IsSelect(sql, letterStart):

                        int j = letterStart - 1;

                        while (IsWhitespace(sql[j])) //? 去空格。
                        {
                            j--;
                        }

                        if (IsUnion(sql, j) || IsUnionAll(sql, j) || IsExcept(sql, j) || IsIntersect(sql, j))
                        {
                            continue;
                        }

                        return letterStart - 1;
                    case 7 when IsDeclare(sql, letterStart):
                        return letterStart - 1;
                    default:
                        break;
                }
            }

            return length;
        }

        /// <summary>
        /// SET ... 分析。
        /// </summary>
        /// <param name="sql">内容。</param>
        /// <param name="startIndex">开始。</param>
        /// <returns></returns>
        private static int IndependentSetRangeAnalysis(string sql, int startIndex)
        {
            bool flag = true;
            bool quotesFlag = false;

            int length = sql.Length;

            int letterStart = 0;

            int bracketLeft = 0;
            int bracketRight = 0;

            for (int i = startIndex; i < length; i++) //? 空白符处理。
            {
                char c = sql[i];

                if (c == '\'')
                {
                    if (quotesFlag) //? 右引号。
                    {
                        quotesFlag = false;

                        continue;
                    }

                    quotesFlag = true;
                }

                if (quotesFlag) //? 占位符。
                {
                    continue;
                }

                if (flag && IsFragment(c))
                {
                    flag = false;

                    letterStart = i;

                    continue;
                }

                if (c == '(')
                {
                    bracketLeft++;
                }
                else if (c == ')')
                {
                    bracketRight++;

                    goto label_check;
                }
                else if (IsWhitespace(c))
                {
                    flag = true;

                    goto label_check;
                }

                continue;

label_check:

                if (letterStart == 0 || bracketLeft != bracketRight)
                {
                    continue;
                }

                switch (i - letterStart)
                {
                    case 4 when IsFrom(sql, letterStart):
                    case 5 when IsWhere(sql, letterStart):
                        return letterStart;
                    default:
                        break;
                }
            }

            return length;
        }

        /// <summary>
        /// 独立查询SQL类型。
        /// </summary>
        private enum IndependentSelectSqlType
        {
            None = 0, //? 未知。
            Normal = 1, //? 常规。
            HorizontalCombination = 2 //? 平级组合。
        }

        /// <summary>
        /// 是独立的查询SQL语句。
        /// </summary>
        /// <param name="sql">内容。</param>
        /// <returns></returns>
        private static IndependentSelectSqlType IndependentSelectSqlAnalysis(string sql)
        {
            int i = 0;

            int length = sql.Length;

            for (; i < length; i++) //? 空白符处理。
            {
                char c = sql[i];

                if (IsWhitespace(c))
                {
                    continue;
                }

                break;
            }

            for (int j = 0; j < 6; j++) //? 不是查询语句。
            {
                if (SelectChars[j].Equals(char.ToLower(sql[i++])))
                {
                    continue;
                }

                return IndependentSelectSqlType.None;
            }

            if (!IsWhitespace(sql[i])) //? 单词界限。
            {
                return IndependentSelectSqlType.None;
            }

            int indexOfCemicolon = sql.IndexOf(';', i);

            if (indexOfCemicolon > -1) //? 分号分割。
            {
                for (int j = indexOfCemicolon + 1; j < length; j++)
                {
                    char c = sql[j];

                    if (IsWhitespace(c))
                    {
                        continue;
                    }

                    return IndependentSelectSqlType.None;
                }
            }

            bool flag = true;
            bool quotesFlag = false;
            bool isHorizontalCombination = false;

            int letterStart = 0;

            int bracketLeft = 0;
            int bracketRight = 0;

            for (; i < length; i++) //? 空白符处理。
            {
                char c = sql[i];

                if (c == '\\') //? 转义。
                {
                    i++;

                    continue;
                }

                if (c == '\'')
                {
                    if (quotesFlag) //? 右引号。
                    {
                        quotesFlag = false;

                        continue;
                    }

                    quotesFlag = true;
                }

                if (quotesFlag) //? 占位符。
                {
                    continue;
                }

                if (flag && IsFragment(c))
                {
                    flag = false;

                    letterStart = i;

                    continue;
                }


                if (c == '(')
                {
                    bracketLeft++;
                }
                else if (c == ')')
                {
                    bracketRight++;

                    goto label_check;
                }
                else
                if (IsWhitespace(c))
                {
                    flag = true;

                    goto label_check;
                }

                continue;
label_check:

                if (letterStart == 0 || bracketLeft != bracketRight)
                {
                    continue;
                }

                switch (i - letterStart)
                {
                    case 4 when IsWith(sql, letterStart) || IsDrop(sql, letterStart):
                    case 5 when IsAlter(sql, letterStart):
                    case 6 when IsDelete(sql, letterStart) || IsInsert(sql, letterStart) || IsUpdate(sql, letterStart):
                    case 7 when IsDeclare(sql, letterStart):
                        return IndependentSelectSqlType.None;
                    case 6 when IsSelect(sql, letterStart):
                        int j = letterStart - 1;

                        while (IsWhitespace(sql[j])) //? 去空格。
                        {
                            j--;
                        }

                        if (IsUnion(sql, j) || IsUnionAll(sql, j) || IsExcept(sql, j) || IsIntersect(sql, j))
                        {
                            isHorizontalCombination = true;

                            break;
                        }

                        return IndependentSelectSqlType.None;
                    default:
                        break;
                }
            }

            return isHorizontalCombination
                ? IndependentSelectSqlType.HorizontalCombination
                : IndependentSelectSqlType.Normal;
        }

        private static readonly char[] SelectChars = new char[] { 's', 'e', 'l', 'e', 'c', 't' };
        private static bool IsSelect(string sql, int startIndex)
        {
            for (int i = 0; i < 6; i++)
            {
                if (SelectChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] InsertChars = new char[] { 'i', 'n', 's', 'e', 'r', 't' };

        private static bool IsInsert(string sql, int startIndex)
        {
            for (int i = 0; i < 6; i++)
            {
                if (InsertChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] UpdateChars = new char[] { 'u', 'p', 'd', 'a', 't', 'e' };
        private static bool IsUpdate(string sql, int startIndex)
        {
            for (int i = 0; i < 6; i++)
            {
                if (UpdateChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] DeleteChars = new char[] { 'd', 'e', 'l', 'e', 't', 'e' };
        private static bool IsDelete(string sql, int startIndex)
        {
            for (int i = 0; i < 6; i++)
            {
                if (DeleteChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] DropChars = new char[] { 'd', 'r', 'o', 'p' };
        private static bool IsDrop(string sql, int startIndex)
        {
            for (int i = 0; i < 6; i++)
            {
                if (DropChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] AlterChars = new char[] { 'a', 'l', 't', 'e', 'r' };

        private static bool IsAlter(string sql, int startIndex)
        {
            for (int i = 0; i < 6; i++)
            {
                if (AlterChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] DeclareChars = new char[] { 'd', 'e', 'c', 'l', 'a', 'r', 'e' };
        private static bool IsDeclare(string sql, int startIndex)
        {
            for (int i = 0; i < 7; i++)
            {
                if (DeclareChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] WithChars = new char[] { 'w', 'i', 't', 'h' };
        private static bool IsWith(string sql, int startIndex)
        {
            for (int i = 0; i < 4; i++)
            {
                if (WithChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] AllChars = new char[] { 'a', 'l', 'l' };
        private static readonly char[] UnionChars = new char[] { 'u', 'n', 'i', 'o', 'n' };

        private static bool IsUnion(string sql, int endIndex)
        {
            if (endIndex < 5)
            {
                return false;
            }

            int startIndex = endIndex - 4;

            for (int i = 0; i < 5; i++)
            {
                if (UnionChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return startIndex == 0 || IsWhitespace(sql[startIndex - 1]);
        }
        private static bool IsUnionAll(string sql, int endIndex)
        {
            if (endIndex < 9)
            {
                return false;
            }
            int startIndex = endIndex - 2;

            for (int i = 0; i < 3; i++)
            {
                if (AllChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            int j = startIndex - 1;

            while (IsWhitespace(sql[j])) //? 去空格。
            {
                j--;
            }

            return IsUnion(sql, j);
        }

        private static readonly char[] IntersectChars = new char[] { 'i', 'n', 't', 'e', 'r', 's', 'e', 'c', 't' };

        private static bool IsIntersect(string value, int endIndex)
        {
            if (endIndex < 9)
            {
                return false;
            }

            int startIndex = endIndex - 8;

            for (int i = 0; i < 9; i++)
            {
                if (IntersectChars[i].Equals(char.ToLower(value[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return startIndex == 0 || IsWhitespace(value[startIndex - 1]);
        }

        private static readonly char[] ExceptChars = new char[] { 'e', 'x', 'c', 'e', 'p', 't' };

        private static bool IsExcept(string value, int endIndex)
        {
            if (endIndex < 6)
            {
                return false;
            }

            int startIndex = endIndex - 5;

            for (int i = 0; i < 6; i++)
            {
                if (ExceptChars[i].Equals(char.ToLower(value[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return startIndex == 0 || IsWhitespace(value[startIndex - 1]);
        }

        private static readonly char[] FromChars = new char[] { 'f', 'r', 'o', 'm' };

        private static bool IsFrom(string sql, int startIndex)
        {
            for (int i = 0; i < 4; i++)
            {
                if (FromChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] DistinctChars = new char[] { 'd', 'i', 's', 't', 'i', 'n', 'c', 't' };

        private static bool IsDistinct(string sql, int startIndex)
        {
            for (int i = 0; i < 8; i++)
            {
                if (DistinctChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] AsChars = new char[] { 'a', 's' };
        private static bool IsAs(string sql, int startIndex)
        {
            for (int i = 0; i < 2; i++)
            {
                if (AsChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] OnChars = new char[] { 'o', 'n' };
        private static bool IsOn(string sql, int startIndex)
        {
            for (int i = 0; i < 2; i++)
            {
                if (OnChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] SetChars = new char[] { 's', 'e', 't' };
        private static bool IsSet(string sql, int startIndex)
        {
            for (int i = 0; i < 3; i++)
            {
                if (SetChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] WhereChars = new char[] { 'w', 'h', 'e', 'r', 'e' };
        private static bool IsWhere(string sql, int startIndex)
        {
            for (int i = 0; i < 5; i++)
            {
                if (WhereChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] LeftChars = new char[] { 'l', 'e', 'f', 't' };
        private static bool IsLeft(string sql, int startIndex)
        {
            for (int i = 0; i < 4; i++)
            {
                if (LeftChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] RightChars = new char[] { 'r', 'i', 'g', 'h', 't' };
        private static bool IsRight(string sql, int startIndex)
        {
            for (int i = 0; i < 5; i++)
            {
                if (RightChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] InnerChars = new char[] { 'i', 'n', 'n', 'e', 'r' };
        private static bool IsInner(string sql, int startIndex)
        {
            for (int i = 0; i < 5; i++)
            {
                if (InnerChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] OuterChars = new char[] { 'o', 'u', 't', 'e', 'r' };
        private static bool IsOuter(string sql, int startIndex)
        {
            for (int i = 0; i < 5; i++)
            {
                if (OuterChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] FullChars = new char[] { 'f', 'u', 'l', 'l' };
        private static bool IsFull(string sql, int startIndex)
        {
            for (int i = 0; i < 4; i++)
            {
                if (FullChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] CrossChars = new char[] { 'c', 'r', 'o', 's', 's' };
        private static bool IsCross(string sql, int startIndex)
        {
            for (int i = 0; i < 4; i++)
            {
                if (CrossChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] JoinChars = new char[] { 'j', 'o', 'i', 'n' };
        private static bool IsJoin(string sql, int startIndex)
        {
            for (int i = 0; i < 4; i++)
            {
                if (JoinChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] GroupChars = new char[] { 'g', 'r', 'o', 'u', 'p' };
        private static bool IsGroup(string sql, int startIndex)
        {
            for (int i = 0; i < 5; i++)
            {
                if (GroupChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] OrderChars = new char[] { 'o', 'r', 'd', 'e', 'r' };
        private static bool IsOrder(string sql, int startIndex)
        {
            for (int i = 0; i < 5; i++)
            {
                if (OrderChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }
        private static bool IsAny(string sql, int startIndex, int count)
        {
            for (int i = startIndex + count - 1; i >= startIndex; i--)
            {
                char c = sql[i];

                if (IsWhitespace(c))
                {
                    continue;
                }

                return c == '*';
            }

            return false;
        }

        private class RangeMatch
        {
            public int Index { get; set; }

            public int Length { get; set; }
        }

        private static Tuple<int, int> AnalysisFields(string sql, out List<RangeMatch> matches)
        {
            int i = 0;
            int length = sql.Length;

            matches = new List<RangeMatch>();

            for (; i < length; i++) //? 空白符处理。
            {
                char c = sql[i];

                if (IsWhitespace(c))
                {
                    continue;
                }

                break;
            }

            bool flag = true;
            bool distinctFlag = true;
            bool quotesFlag = false;

            //? 字段初始。
            int fieldStart = 0;

            int startIndex = 0;

            int letterStart = 0;
            int bracketLeft = 0;
            int bracketRight = 0;

            for (i += 6; i < length; i++) //? 跳过第一个关键字。
            {
                char c = sql[i];

                if (c == '\\') //? 转义。
                {
                    i++;

                    continue;
                }

                if (c == '\'')
                {
                    if (quotesFlag) //? 右引号。
                    {
                        quotesFlag = false;

                        continue;
                    }

                    quotesFlag = true;
                }

                if (quotesFlag) //? 占位符。
                {
                    continue;
                }

                if (bracketLeft == bracketRight && c == ',') //? 字段。
                {
                    matches.Add(new RangeMatch
                    {
                        Index = fieldStart,
                        Length = i - fieldStart
                    });

                    fieldStart = i + 1;
                }
                else if (c == '(')
                {
                    bracketLeft++;
                }
                else if (c == ')')
                {
                    bracketRight++;

                    goto label_check;
                }
                else if (IsWhitespace(c))
                {
                    flag = true;

                    goto label_check;
                }
                else if (flag)
                {
                    flag = false;

                    letterStart = i;
                }

                continue;

label_check:
                if (bracketLeft == bracketRight && letterStart > 0)
                {
                    int offset = i - letterStart;

                    if (offset == 4 && IsFrom(sql, letterStart)) //? from
                    {
                        break;
                    }

                    if (distinctFlag && offset == 8 && IsDistinct(sql, letterStart))
                    {
                        fieldStart = startIndex = i;

                        distinctFlag = false;
                    }
                }
                else if (startIndex == 0)
                {
                    startIndex = i;
                }
            }

            letterStart -= 1; //? 去除最后一个占位符。

            matches.Add(new RangeMatch
            {
                Index = fieldStart,
                Length = letterStart - fieldStart
            });

            return Tuple.Create(startIndex, letterStart);
        }

        private static string MakeName(string str) => Regex.Replace(str, "[^\\w]", "_");

        private static int TryFollowTableAs(string sql, int startIndex, bool allowFrom, out List<RangeMatch> matches)
        {
            matches = new List<RangeMatch>(2);

            int i = startIndex;
            int length = sql.Length;

            int letterStart = 0;
            int bracketLeft = 0;
            int bracketRight = 0;

            bool flag = true;
            bool quotesFlag = false;

            for (; i < length; i++) //? 计算上一次剩余，如：with(lock), table x 
            {
                char c = sql[i];

                if (c == '\\') //? 转义。
                {
                    i++;

                    continue;
                }

                if (c == '\'')
                {
                    if (quotesFlag) //? 右引号。
                    {
                        quotesFlag = false;

                        continue;
                    }

                    quotesFlag = true;
                }

                if (quotesFlag) //? 占位符。
                {
                    continue;
                }

                if (c == '(')
                {
                    if (bracketLeft == bracketRight)
                    {
                        bracketLeft++;

                        goto label_check;
                    }

                    bracketLeft++;

                    continue;
                }

                if (c == ')')
                {
                    bracketRight++;

                    letterStart = i + 1;
                }

                if (bracketLeft > bracketRight)
                {
                    continue;
                }

                if (c == ',') //? 有效的跟随表。
                {
                    goto label_table_as;
                }

                if (flag && IsFragment(c))
                {
                    flag = false;

                    letterStart = i;

                    continue;
                }

                if (letterStart == 0)
                {
                    continue;
                }

                if (IsWhitespace(c))
                {
                    flag = true;

                    goto label_check;
                }

                continue;
label_check:

                switch (i - letterStart)
                {
                    case 4 when IsJoin(sql, letterStart):
                        goto label_table_as;
                    case 4 when IsFrom(sql, letterStart):

                        if (allowFrom)
                        {
                            goto label_table_as;
                        }

                        break;
                    case 3 when IsSet(sql, letterStart):
                    case 5 when IsWhere(sql, letterStart) || IsOrder(sql, letterStart) || IsGroup(sql, letterStart):
                    case 6 when IsSelect(sql, letterStart):
                        return startIndex;
                    default:
                        continue;
                }

                break;
            }

            return startIndex;

label_table_as: //? 表和别名计算。

            flag = true;
            quotesFlag = false;

            letterStart = 0;

            bool asFlag = false;
            bool firstFlag = true;
            bool squareLeftBracketsFlag = false;
            bool squareRightBracketsFlag = false;

            for (i++; i < length; i++) //? 空白符处理。
            {
                char c = sql[i];

                if (c == '\\') //? 转义。
                {
                    i++;

                    continue;
                }

                if (c == '\'')
                {
                    if (quotesFlag) //? 右引号。
                    {
                        quotesFlag = false;

                        continue;
                    }

                    quotesFlag = true;
                }

                if (quotesFlag) //? 占位符。
                {
                    continue;
                }

                if (squareRightBracketsFlag)
                {
                    if (c == '.')
                    {
                        letterStart = i + 1;

                        squareRightBracketsFlag = false;

                        continue;
                    }

                    squareRightBracketsFlag = firstFlag = asFlag = false;

                    matches.Add(new RangeMatch
                    {
                        Index = letterStart,
                        Length = i - letterStart
                    });

                    if (matches.Count == 2)
                    {
                        break;
                    }

                    flag = true;
                    letterStart = 0;
                }

                if (squareLeftBracketsFlag) //? 左中括号的，不限内容。
                {
                    if (c == ']') //? 右中括号。
                    {
                        squareLeftBracketsFlag = false;
                        squareRightBracketsFlag = true;
                    }

                    continue;
                }

                if (c == '[')
                {
                    letterStart = i;

                    flag = false;

                    squareLeftBracketsFlag = true;

                    continue;
                }

                if (c == '.') //? 跳过前面的如：dbo.table => table。
                {
                    letterStart = i + 1;

                    continue;
                }

                if (c == ',') //? 分割。
                {
                    if (letterStart > 0)
                    {
                        matches.Add(new RangeMatch
                        {
                            Index = letterStart,
                            Length = i - letterStart
                        });
                    }

                    break;
                }

                if (c == '(' || c == ')')
                {
                    if (firstFlag) // 如：, (select ...) as x
                    {
                        return startIndex;
                    }

                    break;
                }

                if (flag && IsFragment(c))
                {
                    flag = false;

                    letterStart = i;

                    continue;
                }

                if (letterStart == 0)
                {
                    continue;
                }

                if (IsWhitespace(c))
                {
                    flag = true;

                    goto label_check;
                }

                continue;

label_check:
                if (asFlag || firstFlag)
                {
                    matches.Add(new RangeMatch
                    {
                        Index = letterStart,
                        Length = i - letterStart
                    });

                    firstFlag = false;

                    if (asFlag)
                    {
                        break;
                    }

                    letterStart = 0;

                    continue;
                }

                switch (i - letterStart)
                {
                    case 2 when IsAs(sql, letterStart):

                        letterStart = i + 1;

                        asFlag = true;

                        continue;
                    case 2 when IsOn(sql, letterStart):
                    case 4 when IsLeft(sql, letterStart) || IsJoin(sql, letterStart) || IsFull(sql, letterStart) || IsFrom(sql, letterStart):
                    case 5 when IsInner(sql, letterStart) || IsRight(sql, letterStart) || IsOuter(sql, letterStart) || IsCross(sql, letterStart)
                                || IsWhere(sql, letterStart) || IsOrder(sql, letterStart) || IsGroup(sql, letterStart):
                        return letterStart;
                    default:
                        matches.Add(new RangeMatch
                        {
                            Index = letterStart,
                            Length = i - letterStart
                        });

                        break;
                }

                break;
            }

            return i;
        }
        #endregion

        /// <summary>
        /// SQL 分析。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <returns></returns>
        public string Analyze(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException("语句不能为空或空字符串!");
            }

            return SqlCache.GetOrAdd(sql, Aw_Analyze);
        }

        private static string Aw_Analyze(string sql)
        {
            //? 注解
            sql = PatternAnnotate.Replace(sql, string.Empty);

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new DSyntaxErrorException("未检测到可执行的语句!");
            }

            List<string> characters = new List<string>();

            //? 提取字符串
            sql = PatternCharacter.Replace(sql, item =>
            {
                string value = item.Value;

                var c = value[0];

                characters.Add(value);

                return new string(new char[3] { c, '?', c });
            });

            //? 去除多余的空白符。
            sql = PatternWhitespace.Replace(sql, string.Empty);

            //? Trim
            sql = PatternTrim.Replace(sql, item =>
            {
                Group nameGrp = item.Groups["name"];

                string name = nameGrp.Value;

                StringBuilder sb = new StringBuilder(item.Length + nameGrp.Length + 4);

                return sb.Append('L')
                        .Append(name)
                        .Append('(')
                        .Append('R')
                        .Append(name)
                        .Append(item.Value, nameGrp.Length, item.Length - nameGrp.Length)
                        .Append(')')
                        .ToString();
            });

            int offset = 0;

            StringBuilder sbSql = new StringBuilder(sql.Length + 50);

            List<TableToken> tables = new List<TableToken>();

            HashSet<string> withAs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var withAsMt = PatternWithAs.Match(sql);

            while (withAsMt.Success)
            {
                if (offset > 0)
                {
                    sbSql.Append(Environment.NewLine);
                }

                if (withAsMt.Index > offset) //? 前部分。
                {
#if NETSTANDARD2_1_OR_GREATER
                    sbSql.Append(Done(sql[offset..withAsMt.Index], false));
#else
                    sbSql.Append(Done(sql.Substring(offset, withAsMt.Index - offset), false));
#endif
                }

                var nameGrp = withAsMt.Groups["name"];
                var sqlGrp = withAsMt.Groups["sql"];

                for (int i = 0, length = nameGrp.Captures.Count; i < length; i++)
                {
                    var nameCap = nameGrp.Captures[i];
                    var sqlCap = sqlGrp.Captures[i];

                    if (i == 0)
                    {
                        sbSql.Append(sql, withAsMt.Index, nameCap.Index - withAsMt.Index);
                    }
                    else
                    {
                        sbSql.Append('\x20')
                            .Append(',');
                    }

                    withAs.Add(nameCap.Value); //? 递归查询。

                    sbSql.Append(LeftBracket)
                        .Append(nameCap.Value)
                        .Append(RightBracket)
                        .Append(sql, nameCap.Index + nameCap.Length, sqlCap.Index - nameCap.Index - nameCap.Length)
                        .Append(DoneSelectSql(sqlCap.Value))
                        .Append(')')
                        .Append(Environment.NewLine);
                }

                offset = IndependentSqlAnalysis(sql, withAsMt.Index + withAsMt.Length);

                sbSql.Append(Done(sql.Substring(withAsMt.Index + withAsMt.Length, offset - withAsMt.Index - withAsMt.Length), true));

                withAsMt = withAsMt.NextMatch();

                withAs.Clear(); //? 清空别名集合。
            }

            string wrapSql;

            if (offset == 0)
            {
                wrapSql = Done(sql, false);
            }
            else
            {
                if (sql.Length > offset)
                {
                    sbSql.Append(Environment.NewLine)
#if NETSTANDARD2_1_OR_GREATER
                            .Append(Done(sql[offset..], false));
#else
                            .Append(Done(sql.Substring(offset), false));
#endif
                }

                wrapSql = sbSql.ToString();
            }

            if (tables.Count > 0)
            {
#if NET40
                TableCache.TryAdd(wrapSql, tables.ToReadOnlyList());
#else
                TableCache.TryAdd(wrapSql, tables);
#endif
            }

            if (characters.Count > 0)
            {
                CharacterCache.GetOrAdd(wrapSql, characters);
            }

            return wrapSql;

            string Done(string sqlStr, bool isIndependent)
            {
                if (sqlStr.Length < 7)
                {
                    return sqlStr;
                }


                if (isIndependent)
                {
                    return DoneCompleteSql(DoneIndependentSql(sqlStr));
                }

                int startAt = 0;
                int endIndex = 0;
                int length = sqlStr.Length;

                StringBuilder sbEx = new StringBuilder();

                do
                {
                    endIndex = IndependentSqlAnalysis(sqlStr, startAt);

                    if (startAt > 0)
                    {
                        sbEx.Append(Environment.NewLine);
                    }

#if NETSTANDARD2_1_OR_GREATER
                    string independentSql = sqlStr[startAt..endIndex];
#else
                    string independentSql = sqlStr.Substring(startAt, endIndex - startAt);
#endif
                    sbEx.Append(DoneIndependentSql(independentSql));

                    startAt = endIndex;

                } while (length > endIndex);

                return DoneCompleteSql(sbEx.ToString());
            }

            string DoneSelectSql(string sqlStr)
            {
                int startAt = 0;
                int length = sqlStr.Length;

                StringBuilder sbEx = new StringBuilder(length + 20);

                var match = PatternForm.Match(sqlStr, startAt);

                while (match.Success)
                {
                    var aliasGrp = match.Groups["alias"];
                    var tableGrp = match.Groups["table"];
                    var nameGrp = match.Groups["name"];

                    sbEx.Append(sqlStr, startAt, tableGrp.Index - startAt)
                        .Append(LeftBracket)
                        .Append(nameGrp.Value)
                        .Append(RightBracket);

                    AddTableToken(CommandTypes.Select, nameGrp.Value); //? 非主SQL按照查询语句翻译。

                    if (aliasGrp.Success)
                    {
                        sbEx.Append(sqlStr, tableGrp.Index + tableGrp.Length, aliasGrp.Index - tableGrp.Index - tableGrp.Length)
                            .Append(LeftBracket)
                            .Append(aliasGrp.Value)
                            .Append(RightBracket);
                    }

                    startAt = DoneForm(sbEx, sqlStr, match.Index + match.Length, CommandTypes.Select);

                    match = match.NextMatch();
                }

                if (length > startAt)
                {
                    sbEx.Append(sqlStr, startAt, length - startAt);
                }

                return DoneCompleteSql(sbEx.ToString());
            }

            string DoneCompleteSql(string analyseSql)
            {
                //? 参数处理。
                analyseSql = PatternParameter.Replace(analyseSql, item => string.Concat("{", item.Groups["name"].Value, "}"));

                //? 字段和别名处理。
                analyseSql = PatternAliasField.Replace(analyseSql, item =>
                {
                    var sb = new StringBuilder(item.Length + 4);

                    Group nameGrp = item.Groups["name"];
                    Group aliasGrp = item.Groups["alias"];

                    return sb.Append(LeftBracket)
                          .Append(aliasGrp.Value)
                          .Append("].[")
                          .Append(nameGrp.Value)
                          .Append(RightBracket)
                          .ToString();
                });

                //? 独立参数字段处理
                analyseSql = PatternSingleArgField.Replace(analyseSql, item =>
                {
                    var sb = new StringBuilder(item.Length + 2);

                    Group nameGrp = item.Groups["name"];

                    int startIndex = nameGrp.Index - item.Index + nameGrp.Length;

                    return sb.Append(item.Value, 0, nameGrp.Index - item.Index)
                            .Append(LeftBracket)
                            .Append(nameGrp.Value)
                            .Append(RightBracket)
                            .Append(item.Value, startIndex, item.Length - startIndex)
                            .ToString();
                });

                //? 字段处理。
                analyseSql = PatternField.Replace(analyseSql, item =>
                {
                    var sb = new StringBuilder(item.Length + 2);

                    Group nameGrp = item.Groups["name"];

                    int startIndex = nameGrp.Index - item.Index + nameGrp.Length;

                    return sb.Append(item.Value, 0, nameGrp.Index - item.Index)
                            .Append(LeftBracket)
                            .Append(nameGrp.Value)
                            .Append(RightBracket)
                            .Append(item.Value, startIndex, item.Length - startIndex)
                            .ToString();
                });

                //? 字段处理。
                analyseSql = PatternFieldEmbody.Replace(analyseSql, item =>
                {
                    Group nameGrp = item.Groups["name"];

                    var sb = new StringBuilder(item.Length + 2);

                    if (nameGrp.Index == item.Index && nameGrp.Length == item.Length)
                        return sb.Append(LeftBracket)
                                .Append(nameGrp.Value)
                                .Append(RightBracket)
                                .ToString();

                    int startIndex = nameGrp.Index - item.Index + nameGrp.Length;

                    return sb.Append(item.Value, 0, nameGrp.Index - item.Index)
                             .Append(LeftBracket)
                             .Append(nameGrp.Value)
                             .Append(RightBracket)
                             .Append(item.Value, startIndex, item.Length - startIndex)
                             .ToString();
                });

                //? 字段别名。
                analyseSql = PatternAsField.Replace(analyseSql, item =>
                {
                    Group nameGrp = item.Groups["name"];

                    var sb = new StringBuilder(nameGrp.Length + 2);

                    return sb.Append(item.Value, 0, nameGrp.Index - item.Index)
                            .Append(LeftBracket)
                            .Append(nameGrp.Value)
                            .Append(RightBracket)
                            .ToString();
                });

                return analyseSql;
            }

            string DoneIndependentSql(string sqlStr)
            {
                int startAt = 0;
                int length = sqlStr.Length;

                bool followFlag = true;

                UppercaseString commandType = CommandTypes.Select;

                StringBuilder sbEx = new StringBuilder(sqlStr.Length + 20);

                //? 插入。
                var match = PatternInsert.Match(sqlStr);

                if (match.Success)
                {
                    var nameGrp = match.Groups["name"];
                    var tableGrp = match.Groups["table"];

                    AddTableToken(CommandTypes.Insert, nameGrp.Value);

                    sbEx.Append(sqlStr, 0, tableGrp.Index)
                        .Append(LeftBracket)
                        .Append(nameGrp.Value)
                        .Append(RightBracket);

                    goto label_commandType;
                }
                //? 创建表（指令中可能会包含 DELETE/UPDATE 等关键字，所以必须前置）。
                match = PatternCreate.Match(sqlStr);

                if (match.Success)
                {
                    var tableGrp = match.Groups["table"];
                    var nameGrp = match.Groups["name"];

                    AddTableToken(CommandTypes.Create, nameGrp.Value);

                    sbEx.Append(sqlStr, 0, tableGrp.Index)
                        .Append(LeftBracket)
                        .Append(nameGrp.Value)
                        .Append(RightBracket);

                    startAt = match.Index + match.Length;

                    match = PatternFieldCreate.Match(sqlStr, startAt);

                    while (match.Success)
                    {
                        Group nameSubGrp = match.Groups["name"];

                        sbEx.Append(sqlStr, startAt, nameSubGrp.Index - startAt)
                            .Append(LeftBracket)
                            .Append(nameSubGrp.Value)
                            .Append(RightBracket);

                        startAt = match.Index + match.Length;

                        //? 补全。
                        sbEx.Append(sqlStr, nameSubGrp.Index + nameSubGrp.Length, startAt - nameSubGrp.Index - nameSubGrp.Length);

                        match = match.NextMatch();
                    }

                    goto label_commandType;
                }

                //? 普通删除。
                match = PatternDelete.Match(sqlStr);

                if (match.Success)
                {
                    var tableGrp = match.Groups["table"];
                    var nameGrp = match.Groups["name"];
                    var nicknameGrp = match.Groups["nickname"];
                    var aliasGrp = match.Groups["alias"];

                    AddTableToken(commandType = CommandTypes.Delete, nameGrp.Value);  //? 不需要分析追随表，按照普通分析。

                    sbEx.Append(sqlStr, 0, tableGrp.Index)
                        .Append(LeftBracket)
                        .Append(nameGrp.Value)
                        .Append(RightBracket);

                    if (nicknameGrp.Success)
                    {
                        if (aliasGrp.Success)
                        {
                            sbEx.Append(sqlStr, tableGrp.Index + tableGrp.Length, aliasGrp.Index - tableGrp.Index - tableGrp.Length)
                                    .Append(LeftBracket)
                                    .Append(aliasGrp.Value)
                                    .Append(RightBracket);
                        }
                        else
                        {
                            sbEx.Append(sqlStr, tableGrp.Index + tableGrp.Length, nicknameGrp.Index + nicknameGrp.Length - tableGrp.Index - tableGrp.Length);
                        }
                    }

                    goto label_commandType;
                }

                //? 连表删除操作。
                match = PatternDeleteMulti.Match(sqlStr);

                if (match.Success)
                {
                    var tableGrp = match.Groups["table"];
                    var nameGrp = match.Groups["name"];
                    var nicknameGrp = match.Groups["nickname"];
                    var aliasGrp = match.Groups["alias"];

                    sbEx.Append(sqlStr, 0, tableGrp.Index)
                        .Append(LeftBracket)
                        .Append(nameGrp.Value)
                        .Append(RightBracket);

                    commandType = CommandTypes.Delete;

                    if (nicknameGrp.Success)
                    {
                        AddTableToken(commandType, nameGrp.Value);  //? 不需要分析追随表，按照普通分析。

                        if (aliasGrp.Success)
                        {
                            sbEx.Append(sqlStr, tableGrp.Index + tableGrp.Length, aliasGrp.Index - tableGrp.Index - tableGrp.Length)
                                .Append(LeftBracket)
                                .Append(aliasGrp.Value)
                                .Append(RightBracket);
                        }
                        else
                        {
                            sbEx.Append(sqlStr, tableGrp.Index + tableGrp.Length, nicknameGrp.Index + nicknameGrp.Length - tableGrp.Index - tableGrp.Length);
                        }

                        //? 如：DELETE table1 t1, table2 t2 WHERE t1.id=t2.id AND t1.id=25

                        startAt = DoneForm(sbEx, sqlStr, nicknameGrp.Index + nicknameGrp.Length, commandType);
                    }
                    else
                    {
                        StringBuilder tableEx = new StringBuilder();

                        int tableAt = DoneFormOut(tableEx, sqlStr, match.Index + match.Length, commandType, out List<string> nameAlias);

                        if (!nameAlias.Contains(nameGrp.Value))
                        {
                            AddTableToken(commandType, nameGrp.Value);  //? 不需要分析追随表，按照普通分析。
                        }

                        //? 如：DELETE t1,t2 FROM t1 LEFT JOIN t2 ON t1.id=t2.id WHERE t1.id=25

                        startAt = DoneFormExcept(sbEx, sqlStr, tableGrp.Index + tableGrp.Length, commandType, nameAlias);

                        sbEx.Append(tableEx.ToString());

                        startAt = tableAt;

                        followFlag = false;
                    }

                    goto label_commandType;
                }

                //? 连表更新操作。
                match = PatternUpdate.Match(sqlStr);

                if (match.Success)
                {
                    var tableGrp = match.Groups["table"];
                    var nameGrp = match.Groups["name"];
                    var nicknameGrp = match.Groups["nickname"];
                    var aliasGrp = match.Groups["alias"];

                    sbEx.Append(sqlStr, 0, tableGrp.Index)
                        .Append(LeftBracket)
                        .Append(nameGrp.Value)
                        .Append(RightBracket);

                    commandType = CommandTypes.Update;

                    int setEndAt = IndependentSetRangeAnalysis(sqlStr, match.Index + match.Length);

                    if (nicknameGrp.Success)
                    {
                        if (aliasGrp.Success)
                        {
                            sbEx.Append(sqlStr, tableGrp.Index + tableGrp.Length, aliasGrp.Index - tableGrp.Index - tableGrp.Length)
                                .Append(LeftBracket)
                                .Append(aliasGrp.Value)
                                .Append(RightBracket);
                        }
                        else
                        {
                            sbEx.Append(sqlStr, tableGrp.Index + tableGrp.Length, nicknameGrp.Index + nicknameGrp.Length - tableGrp.Index - tableGrp.Length);
                        }

                        AddTableToken(commandType, nameGrp.Value);  //? 不需要分析追随表，按照普通分析。

                        //? 如： UPDATE db_shop.t_student s JOIN db_shop.t_class c SET s.class_name=c.name,c.stu_name=s.name
                        startAt = DoneForm(sbEx, sqlStr, nicknameGrp.Index + nicknameGrp.Length, commandType);

                        sbEx.Append(sqlStr, startAt, match.Index + match.Length - startAt)
                            .Append(DoneSelectSql(sqlStr.Substring(match.Index + match.Length, setEndAt - match.Index - match.Length)));

                        startAt = setEndAt;
                    }
                    else if (IsFrom(sqlStr, setEndAt))
                    {
                        StringBuilder tableEx = new StringBuilder();

                        int tableAt = DoneFormOut(tableEx, sqlStr, setEndAt, commandType, out List<string> nameAlias);

                        if (!nameAlias.Contains(nameGrp.Value))
                        {
                            AddTableToken(commandType, nameGrp.Value);  //? 不需要分析追随表，按照普通分析。
                        }

                        //? 如： UPDATE c SET c.stu_name='main' FROM db_shop.t_student c
                        startAt = DoneFormExcept(sbEx, sqlStr, tableGrp.Index + tableGrp.Length, commandType, nameAlias);

                        sbEx.Append(sqlStr, startAt, match.Index + match.Length - startAt)
                            .Append(DoneSelectSql(sqlStr.Substring(match.Index + match.Length, setEndAt - match.Index - match.Length)))
                            .Append(tableEx.ToString());

                        startAt = tableAt;

                        followFlag = false;
                    }
                    else
                    {
                        AddTableToken(commandType, nameGrp.Value);  //? 不需要分析追随表，按照普通分析。

                        //? 如： UPDATE db_shop.t_student SET class_name='main'
                        startAt = DoneForm(sbEx, sqlStr, tableGrp.Index + tableGrp.Length, commandType);

                        sbEx.Append(sqlStr, startAt, match.Index + match.Length - startAt)
                            .Append(DoneSelectSql(sqlStr.Substring(match.Index + match.Length, setEndAt - match.Index - match.Length)));

                        startAt = setEndAt;
                    }

                    goto label_commandType;
                }

                //? 修改表。
                match = PatternAlter.Match(sqlStr);

                if (match.Success)
                {
                    var tableGrp = match.Groups["table"];
                    var nameGrp = match.Groups["name"];

                    AddTableToken(CommandTypes.Alter, nameGrp.Value);

                    sbEx.Append(sqlStr, 0, tableGrp.Index)
                        .Append(LeftBracket)
                        .Append(nameGrp.Value)
                        .Append(RightBracket);

                    goto label_commandType;
                }

                //? 删除表。
                match = PatternDrop.Match(sqlStr);

                if (match.Success)
                {
                    var tableGrp = match.Groups["table"];
                    var nameGrp = match.Groups["name"];

                    AddTableToken(CommandTypes.Drop, nameGrp.Value);

                    sbEx.Append(sqlStr, 0, tableGrp.Index)
                        .Append(LeftBracket)
                        .Append(nameGrp.Value)
                        .Append(RightBracket);

                    goto label_commandType;
                }

label_commandType:

                if (length == startAt)
                {
                    return sbEx.ToString();
                }

                if (startAt == 0)
                {
                    startAt = match.Index + match.Length;
                }

                if (match.Success)
                {
                    commandType = CommandTypes.Select; //? 非主SQL按照查询语句翻译。

                    if (followFlag)
                    {
                        startAt = DoneForm(sbEx, sqlStr, startAt, commandType);
                    }
                }

                match = PatternForm.Match(sqlStr, startAt);

                while (match.Success)
                {
                    var aliasGrp = match.Groups["alias"];
                    var tableGrp = match.Groups["table"];
                    var nameGrp = match.Groups["name"];

                    sbEx.Append(sqlStr, startAt, tableGrp.Index - startAt)
                        .Append(LeftBracket)
                        .Append(nameGrp.Value)
                        .Append(RightBracket);

                    AddTableToken(commandType, nameGrp.Value); //? 非主SQL按照查询语句翻译。

                    if (aliasGrp.Success)
                    {
                        sbEx.Append(sqlStr, tableGrp.Index + tableGrp.Length, aliasGrp.Index - tableGrp.Index - tableGrp.Length)
                            .Append(LeftBracket)
                            .Append(aliasGrp.Value)
                            .Append(RightBracket);
                    }

                    startAt = DoneForm(sbEx, sqlStr, match.Index + match.Length, commandType);

                    match = match.NextMatch();
                }

                if (length > startAt)
                {
                    sbEx.Append(sqlStr, startAt, length - startAt);
                }

                return sbEx.ToString();
            }

            int DoneForm(StringBuilder sbEx, string sqlStr, int startAt, UppercaseString commandType)
            {
                while (true)
                {
                    int offsetAt = TryFollowTableAs(sqlStr, startAt, false, out List<RangeMatch> matches);

                    if (offsetAt == startAt)
                    {
                        break;
                    }

                    bool mainFlag = true;

                    foreach (var item in matches)
                    {
                        sbEx.Append(sqlStr, startAt, item.Index - startAt);

                        startAt = item.Index + item.Length;

                        if (sqlStr[item.Index] == LeftBracket && sqlStr[startAt - 1] == RightBracket)
                        {
                            if (mainFlag)
                            {
                                mainFlag = false;

                                AddTableToken(commandType, sqlStr.Substring(item.Index + 1, item.Length - 2));
                            }

                            sbEx.Append(sqlStr, item.Index, item.Length);
                        }
                        else
                        {
                            if (mainFlag)
                            {
                                mainFlag = false;

                                AddTableToken(commandType, sqlStr.Substring(item.Index, item.Length));
                            }

                            sbEx.Append(LeftBracket)
                                .Append(sqlStr, item.Index, item.Length)
                                .Append(RightBracket);
                        }
                    }

                    startAt = offsetAt;
                }

                return startAt;
            }

            int DoneFormOut(StringBuilder sbEx, string sqlStr, int startAt, UppercaseString commandType, out List<string> nameAlias)
            {
                nameAlias = new List<string>(2);

                while (true)
                {
                    int offsetAt = TryFollowTableAs(sqlStr, startAt, true, out List<RangeMatch> matches);

                    if (offsetAt == startAt)
                    {
                        break;
                    }

                    bool mainFlag = true;

                    foreach (var item in matches)
                    {
                        sbEx.Append(sqlStr, startAt, item.Index - startAt);

                        startAt = item.Index + item.Length;

                        if (sqlStr[item.Index] == LeftBracket && sqlStr[startAt - 1] == RightBracket)
                        {
                            string name = sqlStr.Substring(item.Index + 1, item.Length - 2);

                            if (mainFlag)
                            {
                                mainFlag = false;

                                AddTableToken(commandType, name);
                            }
                            else
                            {
                                nameAlias.Add(name);
                            }


                            sbEx.Append(sqlStr, item.Index, item.Length);
                        }
                        else
                        {
                            string name = sqlStr.Substring(item.Index, item.Length);

                            if (mainFlag)
                            {
                                mainFlag = false;

                                AddTableToken(commandType, name);
                            }
                            else
                            {
                                nameAlias.Add(name);
                            }

                            sbEx.Append(LeftBracket)
                                .Append(name)
                                .Append(RightBracket);
                        }
                    }

                    if (offsetAt > startAt)
                    {
                        sbEx.Append(sqlStr, startAt, offsetAt - startAt);
                    }

                    startAt = offsetAt;
                }

                return startAt;
            }

            int DoneFormExcept(StringBuilder sbEx, string sqlStr, int startAt, UppercaseString commandType, List<string> nameAlias)
            {
                while (true)
                {
                    int offsetAt = TryFollowTableAs(sqlStr, startAt, false, out List<RangeMatch> matches);

                    if (offsetAt == startAt)
                    {
                        break;
                    }

                    bool mainFlag = true;
                    bool flag = matches.Count > 1;

                    foreach (var item in matches)
                    {
                        sbEx.Append(sqlStr, startAt, item.Index - startAt);

                        startAt = item.Index + item.Length;

                        if (sqlStr[item.Index] == LeftBracket && sqlStr[startAt - 1] == RightBracket)
                        {
                            if (mainFlag)
                            {
                                mainFlag = false;

                                string name = sqlStr.Substring(item.Index + 1, item.Length - 2);

                                if (flag || !nameAlias.Contains(name))
                                {
                                    AddTableToken(commandType, name);
                                }

                            }

                            sbEx.Append(sqlStr, item.Index, item.Length);
                        }
                        else
                        {
                            if (mainFlag)
                            {
                                mainFlag = false;

                                string name = sqlStr.Substring(item.Index, item.Length);

                                if (flag || !nameAlias.Contains(name))
                                {
                                    AddTableToken(commandType, name);
                                }
                            }

                            sbEx.Append(LeftBracket)
                                .Append(sqlStr, item.Index, item.Length)
                                .Append(RightBracket);
                        }
                    }

                    startAt = offsetAt;
                }

                return startAt;
            }

            void AddTableToken(UppercaseString commandType, string name)
            {
                if (!withAs.Contains(name) && !tables.Exists(x => x.CommandType == commandType && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    tables.Add(new TableToken(commandType, name));
                }
            }
        }


        /// <summary>
        /// SQL 分析（表名称）。
        /// </summary>
        /// <param name="sql">来源于【<see cref="Analyze(string)"/>】的结果。</param>
        /// <returns></returns>
        public IReadOnlyList<TableToken> AnalyzeTables(string sql)
            => TableCache.GetOrAdd(sql, TableToken.None);

        /// <summary>
        /// SQL 分析（参数）。
        /// </summary>
        /// <param name="sql">来源于【<see cref="Analyze(string)"/>】的结果。</param>
        /// <returns></returns>
        public IReadOnlyList<string> AnalyzeParameters(string sql)
        {
            Match match = PatternParameterToken.Match(sql);

            List<string> parameters = new List<string>();

            while (match.Success)
            {
                string name = match.Groups["name"].Value;

                if (!parameters.Contains(name))
                {
                    parameters.Add(name);
                }

                match = match.NextMatch();
            }
#if NET40
            return parameters.ToReadOnlyList();
#else
            return parameters;
#endif
        }

        /// <summary>
        /// 获取符合条件的条数。
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <example>SELECT * FROM Users WHERE Id > 100 => SELECT COUNT(1) FROM Users WHERE Id > 100</example>
        /// <example>SELECT * FROM Users WHERE Id > 100 ORDER BY Id DESC => SELECT COUNT(1) FROM Users WHERE Id > 100</example>
        /// <returns></returns>
        public string ToCountSQL(string sql) => SqlCountCache.GetOrAdd(sql, Aw_ToCountSQL);

        private string Aw_ToCountSQL(string sql)
        {
            var withAsMt = PatternWithAs.Match(sql);

            if (withAsMt.Success)
            {
#if NETSTANDARD2_1_OR_GREATER
                return string.Concat(sql[..(withAsMt.Index + withAsMt.Length)],
                    Environment.NewLine,
                    Aw_ToCountSQL_Simple(sql[(withAsMt.Index + withAsMt.Length)..]));
#else
                return string.Concat(sql.Substring(0, withAsMt.Index + withAsMt.Length),
                    Environment.NewLine,
                    Aw_ToCountSQL_Simple(sql.Substring(withAsMt.Index + withAsMt.Length)));
#endif
            }

            return Aw_ToCountSQL_Simple(sql);
        }

        private static string Aw_ToCountSQL_Simple(string sql)
        {
            IndependentSelectSqlType independentType = IndependentSelectSqlAnalysis(sql);

            if (independentType == IndependentSelectSqlType.None)
            {
                throw new NotSupportedException($"不支持({sql})语句的行数统计!");
            }

            var sb = new StringBuilder();

            if (independentType == IndependentSelectSqlType.HorizontalCombination)
            {
                return sb.Append("SELECT COUNT(1) FROM (")
                    .Append(sql)
                    .Append(") AS xRows")
                    .ToString();
            }

            var colsMt = PatternColumn.Match(sql);

            if (!colsMt.Success)
            {
                return sb.Append("SELECT COUNT(1) FROM (")
                    .Append(sql)
                    .Append(") AS xRows")
                    .ToString();
            }

            var colsGrp = colsMt.Groups["cols"];
            var distinctGrp = colsMt.Groups["distinct"];

            if (distinctGrp.Success)
            {
                if (!TryDistinctField(colsGrp.Value, out string col))
                {
                    return sb.Append("SELECT COUNT(1) FROM (")
                       .Append(sql)
                       .Append(") AS xRows")
                       .ToString();
                }

                sb.Append(sql, 0, distinctGrp.Index)
                    .Append("COUNT(")
                    .Append(distinctGrp.Value)
                    .Append(col)
                    .Append(')');
            }
            else
            {
                sb.Append(sql, 0, colsGrp.Index)
                    .Append("COUNT(1)");
            }

            var orderByMt = PatternOrderBy.Match(sql);

            int subIndex = colsGrp.Index + colsGrp.Length;

            //? 补充 \r\n 换行符处理。
            if (sql[subIndex] == '\n' || sql[subIndex] == '\r')
            {
                do
                {
                    subIndex--;

                } while (sql[subIndex] == '\n' || sql[subIndex] == '\r');

                subIndex++;//? 最后一个非换行字符。
            }

            if (orderByMt.Success)
            {
                sb.Append(sql, subIndex, orderByMt.Index - subIndex);
            }
            else
            {
                sb.Append(sql, subIndex, sql.Length - subIndex);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 生成分页SQL。
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="pageIndex">页码（从“0”开始）</param>
        /// <param name="pageSize">分页条数</param>
        /// <example>SELECT * FROM Users WHERE Id > 100 => PAGING(`SELECT * FROM Users WHERE Id > 100`,<paramref name="pageIndex"/>,<paramref name="pageSize"/>)</example>
        /// <example>SELECT * FROM Users WHERE Id > 100 ORDER BY Id DESC => PAGING(`SELECT * FROM Users WHERE Id > 100`,<paramref name="pageIndex"/>,<paramref name="pageSize"/>,`ORDER BY Id DESC`)</example>
        /// <returns></returns>
        public string ToSQL(string sql, int pageIndex, int pageSize)
            => SqlPagedCache.GetOrAdd(sql, Aw_ToSQL)
                .Replace("{=index}", pageIndex.ToString())
                .Replace("{=size}", pageSize.ToString());

        private string Aw_ToSQL(string sql)
        {
            var withAsMt = PatternWithAs.Match(sql);

            if (withAsMt.Success)
            {
#if NETSTANDARD2_1_OR_GREATER
                string mainSql = Aw_ToSQL_Simple(sql[(withAsMt.Index + withAsMt.Length)..]);

                return string.Concat("P(`", sql[..(withAsMt.Index + withAsMt.Length)],
                    Environment.NewLine,
                    mainSql);
#else
                string mainSql = Aw_ToSQL_Simple(sql.Substring(withAsMt.Index + withAsMt.Length));

                return string.Concat("P(`", sql.Substring(0, withAsMt.Index + withAsMt.Length),
                    Environment.NewLine,
                    mainSql);
#endif
            }
            else
            {
                string mainSql = Aw_ToSQL_Simple(sql);

                return string.Concat("P(`", mainSql);
            }
        }

        private static string Aw_ToSQL_Simple(string sql)
        {
            if (PatternPaging.IsMatch(sql))
            {
                throw new NotSupportedException("请勿重复分页!");
            }

            IndependentSelectSqlType independentType = IndependentSelectSqlAnalysis(sql);

            if (independentType == IndependentSelectSqlType.None)
            {
                throw new NotSupportedException($"不支持({sql})语句的分页!");
            }

            var sb = new StringBuilder();

            if (independentType == IndependentSelectSqlType.HorizontalCombination)
            {
                var tuple = AnalysisFields(sql, out List<RangeMatch> matches);

                StringBuilder sbFields = new StringBuilder();

                sb.Append("SELECT ");

                for (int i = 0; i < matches.Count; i++)
                {
                    var item = matches[i];

                    var match = PatternSingleAsColumn.Match(sql, item.Index, item.Length);

                    if (i > 0)
                    {
                        sb.Append(',');
                        sbFields.Append(',');
                    }

                    sbFields.Append(sql, item.Index, item.Length);

                    if (match.Success)
                    {
                        sb.Append(sql, match.Index, match.Length);
                    }
                    else if (IsAny(sql, item.Index, item.Length))
                    {
                        sb.Length = 0;

                        sb.Append("SELECT * FROM (")
                            .Append(sql);

                        goto label_core;
                    }
                    else
                    {
                        string name = MakeName(sql.Substring(item.Index, item.Length));

                        sb.Append(name);

                        sbFields.Append(" AS ")
                            .Append(name);
                    }
                }

                sb.Append(" FROM (")
                    .Append(sbFields.ToString())
                    .Append(sql, tuple.Item2, sql.Length - tuple.Item2);

label_core:

                return sb.Append(") AS xRows")
                         .Append('`')
                         .Append(',')
                         .Append("{=index}")
                         .Append(',')
                         .Append("{=size}")
                         .Append(')')
                         .ToString();
            }

            var orderByMt = PatternOrderBy.Match(sql);

            if (orderByMt.Success)
            {
                sb.Append(sql, 0, orderByMt.Index)
                    .Append('`')
                    .Append(',')
                    .Append("{=index}")
                    .Append(',')
                    .Append("{=size}")
                    .Append(',')
                    .Append('`')
                    .Append(sql, orderByMt.Index, sql.Length - orderByMt.Index)
                    .Append('`');
            }
            else
            {
                sb.Append(sql)
                    .Append('`')
                    .Append(',')
                    .Append("{=index}")
                    .Append(',')
                    .Append("{=size}");
            }

            return sb.Append(')').ToString();
        }

        /// <summary>
        /// SQL 格式化。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <param name="settings">配置。</param>
        /// <returns></returns>
        public string Format(string sql, ISQLCorrectSettings settings)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            bool flag = CharacterCache.TryGetValue(sql, out List<string> characters);

            //? 分页。
            sql = PatternPaging.Replace(sql, match =>
            {
                var pageIndex = Convert.ToInt32(match.Groups["index"].Value);
                var pageSize = Convert.ToInt32(match.Groups["size"].Value);

                return settings.ToSQL(match.Groups["main"].Value, pageSize, pageIndex * pageSize, match.Groups["orderby"].Value);
            });

            //? 检查 IndexOf 函数，参数处理。
            if (settings.IndexOfSwapPlaces)
            {
                Match match = PatternIndexOf.Match(sql);

                if (match.Success)
                {
                    int offset = 0;

                    StringBuilder sb = new StringBuilder();

                    do
                    {
                        bool commaFlag = false;

                        int startIndex = match.Index + match.Length;

                        sb.Append(sql, offset, startIndex - offset);

                        offset = ParameterAnalysis(sql, startIndex, out var matches);

                        foreach (var item in matches.Take(2).Reverse())
                        {
                            if (commaFlag)
                            {
                                sb.Append(',');
                            }
                            else
                            {
                                commaFlag = true;
                            }

                            sb.Append(sql, item.Index, item.Length);
                        }

                        foreach (var item in matches.Skip(2))
                        {
                            sb.Append(',')
                                .Append(sql, item.Index, item.Length);
                        }

                        match = match.NextMatch();

                    } while (match.Success);

#if NETSTANDARD2_1_OR_GREATER
                    sql = string.Concat(sb.ToString(), sql[offset..]);
#else
                    sql = string.Concat(sb.ToString(), sql.Substring(offset));
#endif
                }
            }

            //? 检查并处理函数名称。
            sql = PatternConvincedMethod.Replace(sql, item =>
            {
                Group nameGrp = item.Groups["name"];

                string name = nameGrp.Value.ToUpper();

                if (name == "LEN" || name == "LENGTH")
                {
                    return settings.Length;
                }

                if (name == "SUBSTRING" || name == "SUBSTR")
                {
                    return settings.Substring;
                }

                if (name == "INDEXOF")
                {
                    return settings.IndexOf;
                }

                return item.Value;
            });

            //? 名称。
            sql = PatternNameToken.Replace(sql, item => settings.Name(item.Groups["name"].Value));

            //? 参数名称。
            sql = PatternParameterToken.Replace(sql, item => settings.ParamterName(item.Groups["name"].Value));

            foreach (IFormatter formatter in settings.Formatters)
            {
                sql = formatter.RegularExpression.Replace(sql, formatter.Evaluator);
            }

            if (flag)
            {
                int offset = 0;

                //? 还原字符串。
                sql = PatternCharacter.Replace(sql, item => characters[offset++]);
            }

            return sql;
        }
    }
}

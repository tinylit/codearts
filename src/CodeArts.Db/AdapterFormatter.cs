using CodeArts.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using static System.Linq.Expressions.Expression;

namespace CodeArts.Db
{
    /// <summary>
    /// 适配器格式化基类。
    /// </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    public abstract class AdapterFormatter<T> where T : AdapterFormatter<T>, IFormatter
    {
        /// <summary>
        /// MVC。
        /// </summary>
        private class Adapter
        {
            /// <summary>
            /// 能否解决。
            /// </summary>
            public Func<Match, bool> CanConvert { get; set; }

            /// <summary>
            /// 解决方案。
            /// </summary>
            public Func<T, Match, string> Convert { get; set; }
        }

        private static readonly List<Adapter> AdapterCache = new List<Adapter>();

        private static MethodInfo GetMethodInfo(Func<Match, string, Group> func) => func.Method;
        private static MethodInfo GetMethodInfo(Func<Group, bool> func) => func.Method;

        /// <summary>
        /// 获取分组信息。
        /// </summary>
        /// <param name="match">匹配项。</param>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        private static Group GetGroup(Match match, string name) => match.Groups[name];

        /// <summary>
        /// 判断匹配的长度是否唯一。
        /// </summary>
        /// <param name="group">分组。</param>
        /// <returns></returns>
        private static bool CheckGroup(Group group) => group.Success && group.Captures.Count == 1;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static AdapterFormatter()
        {
            var contextType = typeof(T);

            var typeStore = TypeStoreItem.Get(contextType);

            var matchType = typeof(Match);
            var groupType = typeof(Group);

            var contextExp = Parameter(contextType, "context");
            var parameterExp = Parameter(matchType, "item");

            var getGroupMethod = GetMethodInfo(GetGroup);

            var checkGroupMethod = GetMethodInfo(CheckGroup);

            typeStore.MethodStores
                .Where(x => x.IsPublic && !x.IsStatic && x.Member.DeclaringType == contextType && x.Member.ReturnType == typeof(string) && x.ParameterStores.Count > 0)
                .OrderByDescending(x => x.ParameterStores.Count)
                .ForEach(x =>
                {
                    if (!x.ParameterStores.All(y => y.ParameterType == matchType || y.ParameterType == groupType || y.ParameterType == typeof(string) || y.ParameterType == typeof(bool)))
                        throw new NotSupportedException("仅支持类型System.Text.RegularExpressions.Match、System.Text.RegularExpressions.Group、System.String、System.Boolean类型的映射。");

                    var conditions = new List<Expression>();
                    var variables = new List<ParameterExpression>();
                    var arguments = new List<Expression>();
                    var expressions = new List<Expression>();

                    x.ParameterStores.ForEach(y =>
                    {
                        if (y.ParameterType == matchType)
                        {
                            arguments.Add(parameterExp);
                        }
                        else
                        {
                            var groupExp = Variable(groupType, y.Name);

                            variables.Add(groupExp);

                            expressions.Add(Assign(groupExp, Call(null, getGroupMethod, parameterExp, Constant(y.Name))));

                            if (y.ParameterType == groupType)
                            {
                                conditions.Add(Property(groupExp, "Success"));

                                arguments.Add(groupExp);
                            }
                            else
                            {
                                if (y.ParameterType == typeof(bool))
                                {
                                    arguments.Add(Property(groupExp, "Success"));
                                }
                                else
                                {
                                    arguments.Add(Property(groupExp, "Value"));

                                    conditions.Add(Call(null, checkGroupMethod, groupExp));
                                }
                            }
                        }
                    });

                    var adapter = new Adapter();

                    var enumerator = conditions.GetEnumerator();

                    if (enumerator.MoveNext())
                    {
                        var condition = enumerator.Current;

                        while (enumerator.MoveNext())
                        {
                            condition = AndAlso(condition, enumerator.Current);
                        }

                        var invoke = Lambda<Func<Match, bool>>(Block(variables, expressions.Concat(new Expression[1] { condition })), parameterExp);

                        adapter.CanConvert = invoke.Compile();
                    }

                    expressions.Add(Call(contextExp, x.Member, arguments));

                    var lamdaExp = Lambda<Func<T, Match, string>>(Block(variables, expressions), contextExp, parameterExp);

                    adapter.Convert = lamdaExp.Compile();

                    AdapterCache.Add(adapter);
                });
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="pattern">正则。</param>
        public AdapterFormatter(string pattern) : this(new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="regex">正则。</param>
        public AdapterFormatter(Regex regex) => RegularExpression = regex;

        /// <summary>
        /// 未能解决时，抛出异常。（默认：true）。
        /// </summary>
        public bool UnsolvedThrowError { get; set; } = true;

        /// <summary>
        /// 表达式。
        /// </summary>
        public Regex RegularExpression { get; }

        /// <summary>
        /// 替换内容。
        /// </summary>
        /// <param name="match">匹配到的内容。</param>
        /// <returns></returns>
        public string Evaluator(Match match)
        {
            foreach (Adapter mvc in AdapterCache)
            {
                if (mvc.CanConvert(match))
                {
                    return mvc.Convert(this as T, match);
                }
            }

            if (UnsolvedThrowError)
            {
                throw new NotSupportedException();
            }

            return match.Value;
        }
    }
}

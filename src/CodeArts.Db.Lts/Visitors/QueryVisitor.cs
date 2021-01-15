using CodeArts.Db.Exceptions;
using CodeArts.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if NET40
using System.Collections.ObjectModel;
#endif
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// 查询访问器。
    /// </summary>
    public sealed class QueryVisitor : SelectVisitor, IQueryVisitor
    {
        private bool useCast = false;

        private List<string> MemberFilters;

        /// <summary>
        /// 类型关系。
        /// </summary>
        private readonly ConcurrentDictionary<Type, Type> CastCache = new ConcurrentDictionary<Type, Type>();
        private readonly ICustomVisitorList visitors;

        /// <inheritdoc />
        public QueryVisitor(ISQLCorrectSettings settings, ICustomVisitorList visitors) : base(settings)
        {
            this.visitors = visitors;
        }

        /// <inheritdoc />
        protected override Expression VisitOfQueryable(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Cast:
                case MethodCall.OfType:

                    Type type = node.Type
                        .GetGenericArguments()
                        .First();

                    if (type.IsValueType || type == typeof(string) || typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        throw new TypeAccessInvalidException($"“{node.Method.Name}”函数泛型参数类型不能是值类型、字符串类型或迭代类型!");
                    }

                    var objExp = node.Arguments[0];

                    var originalType = objExp.Type;

                    if (node.Type == originalType)
                    {
                        return base.Visit(objExp);
                    }

                    useCast = true;

                    CastCache.GetOrAdd(type, _ => TypeToEntryType(originalType));

                    var entry = TypeItem.Get(type);

                    if (MemberFilters is null)
                    {
                        MemberFilters = entry.PropertyStores
                            .Where(x => x.CanRead && x.CanWrite)
                            .Select(x => x.Name.ToLower())
                            .ToList();
                    }
                    else //? 取交集
                    {
                        MemberFilters = MemberFilters
                           .Intersect(entry.PropertyStores
                           .Where(x => x.CanRead && x.CanWrite)
                           .Select(x => x.Name.ToLower()))
                           .ToList();
                    }

                    if (MemberFilters.Count == 0)
                    {
                        throw new DException("未指定查询字段!");
                    }

                    return base.Visit(objExp);
                case MethodCall.DefaultIfEmpty:
                    if (HasDefaultValue)
                    {
                        throw new NotSupportedException($"函数“{node.Method.Name}”仅在表达式链最多只能出现一次！");
                    }

                    if (node.Arguments.Count > 1)
                    {
                        DefaultValue = node.Arguments[1].GetValueFromExpression();
                    }

                    HasDefaultValue = true;

                    return base.Visit(node.Arguments[0]);
                case MethodCall.Last:
                case MethodCall.First:
                case MethodCall.Single:
                case MethodCall.ElementAt:
                    Required = true;
                    goto default;
                default:
                    return base.VisitOfQueryable(node);
            }
        }

        /// <inheritdoc />
        protected override Expression VisitOfSelect(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.TimeOut:

                    TimeOut += (int)node.Arguments[1].GetValueFromExpression();

                    return base.Visit(node.Arguments[0]);
                case MethodCall.NoResultError:

                    if (!Required)
                    {
                        throw new NotSupportedException($"函数“{node.Method.Name}”仅在表达式链以“Last”、“First”、“Single”或“ElementAt”结尾时，可用！");
                    }

                    var valueObj = node.Arguments[1].GetValueFromExpression();

                    if (valueObj is string text)
                    {
                        MissingDataError = text;
                    }

                    return base.Visit(node.Arguments[0]);
                default:
                    return base.VisitOfSelect(node);
            }
        }

        /// <inheritdoc />
        protected override Expression VisitOfQueryableAny(MethodCallExpression node)
        {
            try
            {
                return base.VisitOfQueryableAny(node);
            }
            finally
            {
                if (settings.Engine == DatabaseEngine.Oracle)
                {
                    writer.From();
                    writer.Name("dual");
                }
            }
        }

        /// <inheritdoc />
        protected override Expression VisitOfQueryableAll(MethodCallExpression node)
        {
            try
            {
                return base.VisitOfQueryableAll(node);
            }
            finally
            {
                if (settings.Engine == DatabaseEngine.Oracle)
                {
                    writer.From();
                    writer.Name("dual");
                }
            }
        }

        /// <inheritdoc />
        protected override void WriteMember(string aggregationName, string prefix, string field, string alias)
        {
            base.WriteMember(aggregationName, prefix, field, alias);

            if (field != alias)
            {
                writer.As(alias);
            }
        }

        /// <inheritdoc />
        protected override void WriteMember(string prefix, string field, string alias)
        {
            base.WriteMember(prefix, field, alias);

            if (field != alias)
            {
                writer.As(alias);
            }
        }

        /// <inheritdoc />
        protected override void VisitNewMember(MemberInfo memberInfo, Expression memberExp, Type memberOfHostType)
        {
            base.VisitNewMember(memberInfo, memberExp, memberOfHostType);

            writer.As(memberInfo.Name);
        }

        /// <inheritdoc />
#if NET40
        protected override ReadOnlyCollection<MemberBinding> FilterMemberBindings(ReadOnlyCollection<MemberBinding> bindings)
#else
        protected override IReadOnlyCollection<MemberBinding> FilterMemberBindings(IReadOnlyCollection<MemberBinding> bindings)
#endif
        {
            var vbindings = base.FilterMemberBindings(bindings);

            if (!useCast)
            {
                return vbindings;
            }

            return vbindings
                .Where(x => MemberFilters.Contains(x.Member.Name.ToLower()))
                .ToList()
#if NET40
                .AsReadOnly()
#endif
                ;
        }

        /// <inheritdoc />
        protected override Type TypeToUltimateType(Type entryType)
        {
            var ultimateType = base.TypeToUltimateType(entryType);

            if (useCast && CastCache.TryGetValue(ultimateType, out Type ultimateCastType))
            {
                return ultimateCastType;
            }

            return ultimateType;
        }

        /// <summary>
        /// 获取自定义访问器。
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<ICustomVisitor> GetCustomVisitors() => visitors;

        /// <inheritdoc />
#if NET40
        protected override IDictionary<string, string> FilterMembers(IDictionary<string, string> members)
#else
        protected override IReadOnlyDictionary<string, string> FilterMembers(IReadOnlyDictionary<string, string> members)
#endif
        {
            if (useCast)
            {
                var dic = new Dictionary<string, string>();

                foreach (var kv in members)
                {
                    if (MemberFilters.Contains(kv.Key.ToLower()))
                    {
                        dic.Add(kv.Key, kv.Value);
                    }
                }

                return base.FilterMembers(dic);
            }

            return base.FilterMembers(members);
        }

        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间。
        /// </summary>
        public int? TimeOut { private set; get; }
        /// <summary>
        /// 是否必须。
        /// </summary>
        public bool Required { private set; get; }
        /// <summary>
        /// 有默认值。
        /// </summary>
        public bool HasDefaultValue { private set; get; }
        /// <summary>
        /// 默认值。
        /// </summary>
        public object DefaultValue { private set; get; }
        /// <summary>
        /// 未找到数据异常。
        /// </summary>
        public string MissingDataError { private set; get; }
    }
}

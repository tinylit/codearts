using CodeArts.ORM.Exceptions;
using CodeArts.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// 基础访问器。
    /// </summary>
    public abstract class BaseVisitor : ExpressionVisitor, IDisposable
    {
        #region 匿名内部类
        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldExpression;
            private readonly Expression _newExpression;

            public ReplaceExpressionVisitor(Expression oldExpression, Expression newExpression)
            {
                _oldExpression = oldExpression;
                _newExpression = newExpression;
            }
            public override Expression Visit(Expression node)
            {
                if (_oldExpression == node)
                {
                    return base.Visit(_newExpression);
                }

                return base.Visit(node);
            }
        }
        #endregion

        /// <summary>
        /// 条件。
        /// </summary>
        private readonly SmartSwitch whereSwitch;

        /// <summary>
        /// 在条件中。
        /// </summary>
        private volatile bool isInTheCondition = false;

        /// <summary>
        /// 条件的两端。
        /// </summary>
        private bool isConditionBalance = false;

        /// <summary>
        /// 忽略可空类型。
        /// </summary>
        private bool ignoreNullable = false;

        /// <summary>
        /// 当前表。
        /// </summary>
        private ITableInfo _CurrentRegions;

        private Func<ITableInfo, string> tableGetter;

        private readonly BaseVisitor visitor;
        private readonly bool isNewWriter;

        /// <summary>
        /// 写入器。
        /// </summary>
        protected readonly Writer writer;
        /// <summary>
        /// 矫正配置。
        /// </summary>
        protected readonly ISQLCorrectSettings settings;

        /// <summary>
        /// 空访问器。
        /// </summary>
        private static readonly List<IVisitor> Empty = new List<IVisitor>();

        /// <summary>
        /// 实体类型。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Type> EntryCache = new ConcurrentDictionary<Type, Type>();

        /// <summary>
        /// 表别名。
        /// </summary>
        internal readonly ConcurrentDictionary<Type, string> AliasCache = new ConcurrentDictionary<Type, string>();

        /// <summary>
        /// 是否条件反转。
        /// </summary>
        public bool ReverseCondition => writer.ReverseCondition;

        /// <summary>
        /// inherit。
        /// </summary>
        protected BaseVisitor(ISQLCorrectSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            this.writer = CreateWriter(settings, CreateWriterMap(settings), new Dictionary<string, object>());

            whereSwitch = new SmartSwitch(writer.Where, writer.And);
        }

        /// <summary>
        /// inherit。
        /// </summary>
        protected BaseVisitor(BaseVisitor visitor, bool isNewWriter = false)
        {
            this.visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
            this.isNewWriter = isNewWriter;

            var writer = visitor.writer;

            this.settings = visitor.settings;

            if (isNewWriter)
            {
                this.writer = CreateWriter(settings, CreateWriterMap(settings), writer.Parameters);
            }
            else
            {
                this.writer = writer;
            }


            whereSwitch = new SmartSwitch(this.writer.Where, this.writer.And);
        }

        /// <summary>
        /// 能解决的表达式。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual bool CanResolve(MethodCallExpression node) => true;

        private static readonly Type BaseVisitorType = typeof(BaseVisitor);

        private static readonly ConcurrentDictionary<Type, bool> CanResolveCache = new ConcurrentDictionary<Type, bool>();
        private static MethodInfo GetMethodInfo(Func<MethodCallExpression, bool> func) => func.Method;

        /// <summary>
        /// 启动。
        /// </summary>
        /// <param name="node">分析表达式。</param>
        /// <returns></returns>
        public virtual Expression Startup(Expression node)
        {
            try
            {
                switch (node)
                {
                    case MethodCallExpression callExpression:
                        if (CanResolve(callExpression))
                        {
                            return StartupCore(callExpression);
                        }

                        throw new NotSupportedException();
                    default:
                        if (CanResolveCache.GetOrAdd(GetType(), _ =>
                        {
                            var method = GetMethodInfo(CanResolve);

                            return method.DeclaringType == BaseVisitorType;
                        }))
                        {
                            return Visit(node);
                        }

                        throw new NotSupportedException();
                }
            }
            finally
            {
                if (isNewWriter)
                {
                    visitor.writer.Write(ToSQL());
                }
            }
        }

        /// <summary>
        /// 启动核心流程。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression StartupCore(MethodCallExpression node)
        {
            return base.Visit(node);
        }

        /// <summary>
        /// 创建写入映射关系
        /// </summary>
        /// <param name="settings">修正配置</param>
        /// <returns></returns>
        protected virtual IWriterMap CreateWriterMap(ISQLCorrectSettings settings) => visitor?.CreateWriterMap(settings) ?? new WriterMap(settings);

        /// <summary>
        /// 创建写入流。
        /// </summary>
        /// <param name="settings">修正配置。</param>
        /// <param name="writeMap">写入映射关系。</param>
        /// <param name="parameters">参数。</param>
        /// <returns></returns>
        protected virtual Writer CreateWriter(ISQLCorrectSettings settings, IWriterMap writeMap, Dictionary<string, object> parameters) => visitor?.CreateWriter(settings, writeMap, parameters) ?? new Writer(settings, writeMap, parameters);

        /// <inheritdoc />
        protected virtual IEnumerable<IVisitor> GetCustomVisitors() => visitor?.GetCustomVisitors() ?? Empty;

        #region 辅佐。
        /// <inheritdoc />
        protected ITableInfo MakeTableInfo(Type paramererType)
        {
            Type entryType = TypeToUltimateType(paramererType);

            if (_CurrentRegions is null)
            {
                return _CurrentRegions = MapperRegions.Resolve(entryType);
            }

            return _CurrentRegions.TableType == entryType ? _CurrentRegions : _CurrentRegions = MapperRegions.Resolve(entryType);
        }

        /// <summary>
        /// 表名称。
        /// </summary>
        /// <param name="tableInfo">表信息。</param>
        /// <returns></returns>
        protected string TableName(ITableInfo tableInfo) => tableGetter?.Invoke(tableInfo) ?? tableInfo.TableName;

        /// <summary>
        /// 获取最根本的类型。
        /// </summary>
        /// <returns></returns>
        protected virtual Type TypeToUltimateType(Type entryType) => TypeToEntryType(entryType, true);

        /// <summary>
        /// 从委托或表达式中获取实体类型。
        /// </summary>
        /// <param name="delegateTypeOrExpressionType">委托或表达式类型。</param>
        /// <param name="throwsError">不符合表类型时，是否抛出异常。</param>
        /// <returns></returns>
        protected virtual Type DelegateTypeOrExpressionTypeToEntryType(Type delegateTypeOrExpressionType, bool throwsError = true)
        {
            while (delegateTypeOrExpressionType.IsSubclassOf(typeof(Expression)) || delegateTypeOrExpressionType.IsGenericType && delegateTypeOrExpressionType.GetGenericTypeDefinition() == typeof(Func<,>))
            {
                if (delegateTypeOrExpressionType.IsGenericType)
                {
                    delegateTypeOrExpressionType = delegateTypeOrExpressionType
                        .GetGenericArguments()
                        .First(item => item.IsClass);
                }
                else
                {
                    delegateTypeOrExpressionType = delegateTypeOrExpressionType.BaseType;
                }
            }

            return TypeToUltimateType(delegateTypeOrExpressionType);
        }

        /// <summary>
        /// 获取真实表类型。
        /// </summary>
        /// <param name="repositoryType">类型。</param>
        /// <param name="throwsError">不符合表类型时，是否抛出异常。</param>
        /// <returns></returns>
        protected virtual Type TypeToEntryType(Type repositoryType, bool throwsError = true)
        {
            if (EntryCache.TryGetValue(repositoryType, out Type value))
            {
                return value;
            }

            Type baseType = repositoryType;

            while (baseType.IsQueryable())
            {
                if (baseType.IsGenericType)
                {
                    foreach (Type type in baseType.GetGenericArguments())
                    {
                        if (type.IsValueType || !type.IsClass || type == typeof(string))
                        {
                            continue;
                        }

                        return EntryCache.GetOrAdd(repositoryType, type);
                    }
                }

                baseType = baseType.BaseType;
            };

            if (baseType is null || baseType.IsValueType || !baseType.IsClass || baseType == typeof(string))
            {
                if (throwsError)
                {
                    throw new TypeAccessInvalidException($"访问类型({repositoryType.Namespace}.{repositoryType.Name})无效!");
                }

                return null;
            }

            if (typeof(IEnumerable).IsAssignableFrom(baseType))
            {
                throw new TypeAccessInvalidException($"访问类型({repositoryType.Namespace}.{repositoryType.Name})的泛型参数或基类({baseType.Namespace}.{baseType.Name})是迭代类型,不被支持!");
            }

            return EntryCache.GetOrAdd(repositoryType, baseType);
        }

        /// <summary>
        /// 尝试获取表别名。
        /// </summary>
        /// <param name="entryType">表类型。</param>
        /// <param name="parameterName">参数名称。</param>
        /// <param name="check">检查。</param>
        /// <param name="aliasName">别名。</param>
        /// <returns></returns>
        protected internal virtual bool TryGetEntryAlias(Type entryType, string parameterName, bool check, out string aliasName)
        {
            if (AliasCache.TryGetValue(entryType, out aliasName))
            {
                if (!check || parameterName is null || parameterName.Length == 0 || parameterName == aliasName)
                {
                    return true;
                }
            }

            if (visitor is null)
            {
                return false;
            }

            return visitor.TryGetEntryAlias(entryType, parameterName, check, out aliasName);
        }

        /// <summary>
        /// 获取表别名。
        /// </summary>
        /// <param name="parameterType">表类型。</param>
        /// <param name="parameterName">参数名称。</param>
        /// <returns></returns>
        protected virtual string GetEntryAlias(Type parameterType, string parameterName)
        {
            Type entryType = DelegateTypeOrExpressionTypeToEntryType(parameterType);

            if (TryGetEntryAlias(entryType, parameterName, true, out string aliasName) || TryGetEntryAlias(entryType, parameterName, false, out aliasName))
            {
                return aliasName;
            }

            return AliasCache.GetOrAdd(entryType, parameterName);
        }

        /// <summary>
        /// 条件反转。
        /// </summary>
        /// <returns></returns>
        protected T InvertWhere<T>(Func<T> invoke)
        {
            try
            {
                writer.ReverseCondition ^= true;

                return invoke.Invoke();
            }
            finally
            {
                writer.ReverseCondition ^= true;
            }
        }

        /// <summary>
        /// 条件反转。
        /// </summary>
        /// <returns></returns>
        protected void InvertWhere(Action invoke)
        {
            try
            {
                writer.ReverseCondition ^= true;

                invoke.Invoke();
            }
            finally
            {
                writer.ReverseCondition ^= true;
            }
        }

        /// <summary>
        /// 业务流程。
        /// </summary>
        /// <param name="work">执行。</param>
        /// <param name="ready">准备工作。</param>
        protected void Workflow(Action<bool> work, Action ready = null)
        {
            var index = writer.Length;
            var length = writer.Length;
            var appendAt = writer.AppendAt;

            if (appendAt > -1)
            {
                index -= (index - appendAt);
            }

            ready?.Invoke();

            writer.AppendAt = index;

            work.Invoke(writer.Length > length);

            if (appendAt > -1)
            {
                appendAt += writer.Length - length;
            }

            writer.AppendAt = appendAt;
        }

        /// <summary>
        /// 业务流程。
        /// </summary>
        /// <param name="work">执行。</param>
        /// <param name="ready">准备工作。</param>
        protected void Workflow(Action work, Action ready = null)
        {
            var index = writer.Length;
            var length = writer.Length;
            var appendAt = writer.AppendAt;

            if (appendAt > -1)
            {
                index -= (index - appendAt);
            }

            ready?.Invoke();

            writer.AppendAt = index;

            work.Invoke();

            if (appendAt > -1)
            {
                appendAt += writer.Length - length;
            }

            writer.AppendAt = appendAt;
        }

        #endregion

        /// <summary>
        /// 过来成员
        /// </summary>
        /// <param name="bindings">成员集合</param>
        /// <returns></returns>
#if NET40
        protected virtual ICollection<MemberBinding> FilterMemberBindings(ReadOnlyCollection<MemberBinding> bindings) => bindings;
#else

        protected virtual IReadOnlyCollection<MemberBinding> FilterMemberBindings(ReadOnlyCollection<MemberBinding> bindings) => bindings;
#endif

        /// <summary>
        /// 变量成员。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitMemberIsVariable(MemberExpression node)
        {
            var value = node.GetValueFromExpression();

            if (value is null)
            {
                if (ignoreNullable)
                {
                    return node;
                }
            }
            else if (value is IQueryable queryable)
            {
                base.Visit(queryable.Expression);

                return node;
            }
            else if (value is IExecuteable executeable)
            {
                base.Visit(executeable.Expression);

                return node;
            }

            if (isConditionBalance && value.Equals(!writer.ReverseCondition))
            {
                return node;
            }

            if (node.Expression is null || node.Expression.NodeType == ExpressionType.MemberAccess && (node.Type.IsValueType || node.Expression.Type == node.Type))
            {
                writer.Parameter(string.Concat("__variable_", node.Member.Name.ToLower()), value);
            }
            else
            {
                writer.Parameter(node.Member.Name, value);
            }

            if (isConditionBalance && node.IsBoolean())
            {
                writer.Equal();

                writer.BooleanFalse();
            }

            return node;
        }

        /// <summary>
        /// 成员依赖于参数成员（参数类型是对象）。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitMemberIsDependOnParameterTypeIsObject(MemberExpression node)
        {
            writer.Limit(GetEntryAlias(node.Type, node.Member.Name));

            return node;
        }

        /// <summary>
        /// 成员依赖于参数成员（参数类型是值类型或字符串类型）。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitMemberIsDependOnParameterTypeIsPlain(MemberExpression node)
        {
            var tableInfo = MakeTableInfo(node.Expression.Type);

            if (!tableInfo.ReadOrWrites.TryGetValue(node.Member.Name, out string value))
            {
                throw new DSyntaxErrorException($"“{node.Member.Name}”不可读写!");
            }

            writer.Name(value);

            if (isConditionBalance && node.IsBoolean())
            {
                writer.Equal();

                writer.BooleanTrue();
            }

            return node;
        }

        /// <summary>
        /// 过滤成员。
        /// </summary>
        /// <param name="members">成员。</param>
        /// <returns></returns>
#if NET40
        protected virtual IDictionary<string, string> FilterMembers(IDictionary<string, string> members) => members;
#else
        protected virtual IReadOnlyDictionary<string, string> FilterMembers(IReadOnlyDictionary<string, string> members) => members;
#endif
        /// <summary>
        /// 写入成员。
        /// </summary>
        /// <param name="prefix">前缀。</param>
        /// <param name="field">字段。</param>
        /// <param name="alias">别名。</param>
        protected virtual void WriteMember(string prefix, string field, string alias)
        {
            writer.Name(prefix, field);
        }

        /// <summary>
        /// 写入指定成员
        /// </summary>
        /// <param name="prefix">前缀</param>
        /// <param name="members">成员集合</param>
        protected virtual void WriteMembers(string prefix, IEnumerable<KeyValuePair<string, string>> members)
        {
            var enumerator = members.GetEnumerator();

            if (enumerator.MoveNext())
            {
                WriteMember(prefix, enumerator.Current.Value, enumerator.Current.Key);

                while (enumerator.MoveNext())
                {
                    writer.Delimiter();

                    WriteMember(prefix, enumerator.Current.Value, enumerator.Current.Key);
                }
            }
            else
            {
                throw new DException("未指定查询字段!");
            }
        }

        /// <summary>
        /// 成员依赖于参数成员。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitMemberIsDependOnParameter(MemberExpression node)
        {
            if (node.Type.IsValueType || node.Type == typeof(string))
            {
                return VisitMemberIsDependOnParameterTypeIsPlain(node);
            }
            else
            {
                return VisitMemberIsDependOnParameterTypeIsObject(node);
            }
        }

        /// <summary>
        /// 叶子成员依赖于对象。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitMemberLeavesIsObject(MemberExpression node)
        {
            var regions = MakeTableInfo(node.Type);

            var prefix = GetEntryAlias(regions.TableType, node.Member.Name);

            WriteMembers(prefix, FilterMembers(regions.ReadOrWrites));

            return node;
        }

        /// <summary>
        /// 属性或字段。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitMemberIsPropertyOrField(MemberExpression node)
        {
            return VisitMemberIsVariable(node);
        }

        /// <summary>
        /// inherit。
        /// </summary>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            if (IsPlainVariable(node))
            {
                return VisitMemberIsVariable(node);
            }

            if (node.Expression is null)
            {
                return VisitMemberIsPropertyOrField(node);
            }

            if (node.IsLength())
            {
                writer.LengthMethod();
                writer.OpenBrace();

                try
                {
                    return base.VisitMember(node);
                }
                finally
                {
                    writer.CloseBrace();
                }
            }

            switch (node.Expression)
            {
                case MemberExpression member when member.IsNullable():
                    if (node.IsValue())
                    {
                        return VisitMember(member);
                    }

                    if (node.IsHasValue())
                    {
                        try
                        {
                            return VisitMember(member);
                        }
                        finally
                        {
                            writer.IsNotNull();
                        }
                    }
                    return node;
                case ParameterExpression parameter:

                    if (node.Type.IsValueType || node.Type == typeof(string))
                    {
                        VisitParameter(parameter);

                        return VisitMemberIsDependOnParameter(node);
                    }

                    return VisitMemberLeavesIsObject(node);
                case MemberExpression member:
                    VisitMemberIsDependOnParameter(member);
                    goto default;
                default:
                    return VisitMemberIsDependOnParameter(node);
            }
        }

        /// <inheritdoc />
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            var bindings = FilterMemberBindings(node.Bindings);

            var enumerator = bindings.GetEnumerator();

            if (enumerator.MoveNext())
            {
                base.VisitMemberBinding(enumerator.Current);

                while (enumerator.MoveNext())
                {
                    writer.Delimiter();

                    base.VisitMemberBinding(enumerator.Current);
                }

                return node;
            }
            else
            {
                throw new DException("未指定查询字段!");
            }

        }

        /// <inheritdoc />
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            VisitCheckIfSubconnection(node.Expression);

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression node)
        {
            writer.Limit(GetEntryAlias(node.Type, node.Name));

            return node;
        }

        /// <summary>
        /// Invoke
        /// </summary>
        /// <param name="node">参数</param>
        /// <returns></returns>
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            return base.Visit(node.Expression);
        }

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.IsBoolean())
            {
                throw new DSyntaxErrorException("禁止使用布尔常量作为条件语句或结果!");
            }

            var value = node.Value as ConstantExpression;

            writer.Parameter((value ?? node).GetValueFromExpression());

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (node.Parameters.Count > 1)
            {
                throw new DSyntaxErrorException("不支持多个参数!");
            }

            if (node.Body.NodeType == ExpressionType.New && node.Body is NewExpression newExp)
            {
                if (newExp.Arguments.Any(x => x.NodeType == ExpressionType.MemberAccess && (x is MemberExpression member) && member.Expression?.NodeType == ExpressionType.MemberAccess))
                {
                    base.Visit(node.Body);

                    return node;
                }
            }

            var parameter = node.Parameters[0];

            var parameterType = TypeToUltimateType(parameter.Type);

            AliasCache.TryAdd(parameterType, parameter.Name);

            base.Visit(node.Body);

            return node;
        }

        #region MethodCallExpression
        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var declaringType = node.Method.DeclaringType;

            if (declaringType == typeof(Queryable))
            {
                return VisitOfQueryable(node);
            }

            if (declaringType == typeof(SelectExtentions))
            {
                return VisitOfSelect(node);
            }

            if (declaringType == typeof(Enumerable))
            {
                return VisitOfEnumerable(node);
            }

            if (declaringType == typeof(string))
            {
                return VisitOfString(node);
            }

            if (typeof(IEnumerable).IsAssignableFrom(declaringType))
            {
                return VisitOfIEnumerable(node);
            }

            return VisitByCustom(node);
        }

        /// <summary>
        ///  System.Linq.Queryable 的函数。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfQueryable(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Any:
                    return VisitOfQueryableAny(node);
                case MethodCall.All:
                    return VisitOfQueryableAll(node);
                case MethodCall.Contains:
                    return VisitOfQueryableContains(node);
                case MethodCall.Where:
                case MethodCall.TakeWhile:
                    return VisitCondition(node);
                case MethodCall.SkipWhile:
                    return InvertWhere(() => VisitCondition(node));
                case MethodCall.Union:
                case MethodCall.Concat:
                case MethodCall.Except:
                case MethodCall.Intersect:
                    return VisitCombination(node);
                default:
                    throw new NotSupportedException($"类型“{node.Method.DeclaringType}”的函数“{node.Method.Name}”不被支持!");
            }
        }

        /// <summary>
        ///  System.Linq.Queryable 的Any函数。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfQueryableAny(MethodCallExpression node)
        {
            if (!isInTheCondition)
            {
                writer.Write("CASE WHEN ");
            }

            using (var visitor = new AnyVisitor(this))
            {
                visitor.Startup(node);
            };

            if (!isInTheCondition)
            {
                writer.Write(" THEN ");
                writer.Parameter("__variable_true", true);
                writer.Write(" ELSE ");
                writer.Parameter("__variable_false", false);
                writer.Write(" END");
            }

            return node;
        }

        /// <summary>
        ///  System.Linq.Queryable 的All函数。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfQueryableAll(MethodCallExpression node)
        {
            writer.Write("CASE WHEN ");

            using (var visitor = new AllVisitor(this))
            {
                visitor.Startup(node);
            };

            writer.Write(" THEN ");
            writer.Parameter("__variable_true", true);
            writer.Write(" ELSE ");
            writer.Parameter("__variable_false", false);
            writer.Write(" END");

            return node;
        }

        /// <summary>
        ///  System.Linq.Queryable 的Contains函数。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfQueryableContains(MethodCallExpression node)
        {
            using (var visitor = new ContainsVisitor(this))
            {
                visitor.Startup(node);
            }

            return node;
        }

        /// <summary>
        /// 使用 System.Linq.SelectExtentions 的函数。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitOfSelect(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.From:
                    var objExp = node.Arguments[1];

                    if (!IsPlainVariable(objExp))
                    {
                        throw new NotSupportedException("函数“From”的参数必须是常量!");
                    }

                    var valueObj = objExp.GetValueFromExpression();

                    if (valueObj is Func<ITableInfo, string> tableInfo)
                    {
                        tableGetter = tableInfo;

                        return node;
                    }

                    throw new NotSupportedException("函数“From”的参数类型必须是“System.Func<ITableInfo, string>”委托!");
                default:
                    throw new NotSupportedException($"类型“{node.Method.DeclaringType}”的函数“{node.Method.Name}”不被支持!");
            }
        }

        /// <summary>
        /// System.Linq.Enumerable 的函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfEnumerable(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Any:
                    return VisitOfEnumerableAny(node);
                case MethodCall.Contains:
                    return VisitOfEnumerableContains(node);
                default:
                    return VisitByCustom(node);
            }
        }

        /// <summary>
        /// System.Collections.IEnumerable 的函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfIEnumerable(MethodCallExpression node) => VisitOfEnumerable(node);

        private void BooleanFalse(bool allwaysFalse)
        {
            if (allwaysFalse || !writer.ReverseCondition)
            {
                writer.BooleanTrue();

                writer.Equal();

                writer.BooleanFalse();
            }
        }

        /// <summary>
        /// 是普通变量。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <param name="depthVerification">深度验证。</param>
        /// <returns></returns>
        protected virtual bool IsPlainVariable(Expression node, bool depthVerification = false)
        {
            if (node.NodeType == ExpressionType.Constant)
            {
                return true;
            }

            if (node.NodeType == ExpressionType.Parameter)
            {
                return false;
            }

            switch (node)
            {
                case MemberExpression member:
                    return member.Expression is null || IsPlainVariable(member.Expression);
                case MethodCallExpression method when method.Object is null || IsPlainVariable(method.Object, depthVerification):
                    return method.Arguments.Count == 0 || method.Arguments.All(arg => IsPlainVariable(arg, depthVerification));
                case BinaryExpression binary when depthVerification:
                    return IsPlainVariable(binary.Left, depthVerification) && IsPlainVariable(binary.Right, depthVerification);
                case LambdaExpression lambda when lambda.Parameters.Count == 0:
                    return IsPlainVariable(lambda.Body, depthVerification);
                default:
                    return false;
            }
        }

        /// <summary>
        /// System.Linq.Enumerable 的Any函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfEnumerableAny(MethodCallExpression node)
        {
            var enumerable = (IEnumerable)(node.Object ?? node.Arguments[0]).GetValueFromExpression();

            var enumerator = enumerable.GetEnumerator();

            int index = node.Object is null ? 1 : 0;

            if (node.Arguments.Count == index)
            {
                if (enumerator.MoveNext() ^ writer.ReverseCondition)
                {
                    return node;
                }

                BooleanFalse(true);

                return node;
            }

            var lambda = node.Arguments[index] as LambdaExpression;

            var parameterExp = lambda.Parameters[0];

            void Visit(object value)
            {
                var constantExp = Expression.Constant(value, parameterExp.Type);

                base.Visit(new ReplaceExpressionVisitor(parameterExp, constantExp)
                    .Visit(lambda.Body));
            }

            if (enumerator.MoveNext())
            {
                if (enumerator.Current == null)
                {
                    throw new ArgumentNullException(parameterExp.Name);
                }

                writer.OpenBrace();

                Visit(enumerator.Current);

                while (enumerator.MoveNext())
                {
                    if (enumerator.Current == null)
                    {
                        throw new ArgumentNullException(parameterExp.Name);
                    }

                    writer.Or();

                    Visit(enumerator.Current);
                }

                writer.CloseBrace();

                return node;
            }

            BooleanFalse(false);

            return node;
        }

        /// <summary>
        /// System.Linq.Enumerable 的Contains函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfEnumerableContains(MethodCallExpression node)
        {
            int index = node.Object is null ? 1 : 0;

            Workflow(whereIsNotEmpty =>
            {
                if (whereIsNotEmpty)
                {
                    Visit(node.Arguments[index]);
                }
                else
                {
                    BooleanFalse(false);
                }

            }, () =>
              {
                  var enumerable = (IEnumerable)(node.Object ?? node.Arguments[0]).GetValueFromExpression();
                  var enumerator = enumerable.GetEnumerator();

                  if (enumerator.MoveNext())
                  {
                      int parameterCount = 0;

                      writer.Contains();
                      writer.OpenBrace();
                      writer.Parameter(enumerator.Current);

                      while (enumerator.MoveNext())
                      {
                          if (parameterCount < 256)
                          {
                              writer.Delimiter();
                          }
                          else
                          {
                              parameterCount = 0;

                              writer.CloseBrace();
                              writer.WhiteSpace();
                              writer.Write("OR");
                              writer.WhiteSpace();

                              base.Visit(node.Arguments[index]);

                              writer.Contains();
                              writer.OpenBrace();
                          }

                          writer.Parameter(enumerator.Current);
                      }

                      writer.CloseBrace();
                  }
              });

            return node;
        }

        /// <summary>
        /// System.String 的函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfString(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Contains:
                case MethodCall.EndsWith:
                case MethodCall.StartsWith:
                    return VisitLike(node);
                case MethodCall.IsNullOrEmpty:
                    return VisitIsEmpty(node);
                case MethodCall.Replace:
                    return VisitToReplace(node);
                case MethodCall.Substring:
                    return VisitToSubstring(node);
                case MethodCall.ToUpper:
                case MethodCall.ToLower:
                    return VisitToCaseConversion(node);
                case MethodCall.Trim:
                case MethodCall.TrimEnd:
                case MethodCall.TrimStart:
                    return VisitToTrim(node);
                case MethodCall.IndexOf when node.Arguments.Count > 1:
                    return VisitByIndexOfWithLimit(node);
                case MethodCall.IndexOf:
                    return VisitByIndexOf(node);
                default:
                    return VisitByCustom(node);
            }
        }

        /// <summary>
        /// System.String 的 Contains、StartsWith、EndsWith函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitLike(MethodCallExpression node)
        {
            if (node.Arguments.Count > 1)
            {
                throw new DSyntaxErrorException($"仅支持参数类型为“System.String”的({node.Method.Name}(String))方法。");
            }

            if (IsPlainVariable(node.Arguments[0]))
            {
                return VisitLikeByVariable(node);
            }

            return VisitLikeByExpression(node);
        }

        /// <summary>
        /// System.String 的 Contains、StartsWith、EndsWith函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitLikeByVariable(MethodCallExpression node)
        {
            var objExp = node.Arguments[0];

            var value = objExp.GetValueFromExpression();

            if (value is null)
            {
                return node;
            }

            if (!(value is string text))
            {
                throw new DSyntaxErrorException($"仅支持参数类型为“System.String”的({node.Method.Name})方法。");
            }

            base.Visit(node.Object);

            if (text.Length == 0)
            {
                InvertWhere(writer.IsNull);
            }
            else
            {
                writer.Like();

                if (node.Method.Name == MethodCall.EndsWith || node.Method.Name == MethodCall.Contains)
                {
                    text = "%" + text;
                }

                if (node.Method.Name == MethodCall.StartsWith || node.Method.Name == MethodCall.Contains)
                {
                    text += "%";
                }

                if (objExp is MemberExpression member)
                {
                    writer.Parameter(member.Member.Name, text);
                }
                else
                {
                    writer.Parameter(text);
                }
            }

            return node;
        }

        /// <summary>
        /// System.String 的 Contains、StartsWith、EndsWith函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitLikeByExpression(MethodCallExpression node)
        {
            base.Visit(node.Object);

            writer.Like();

            if (settings.Engine == DatabaseEngine.MySQL)
            {
                writer.Write("CONCAT");
                writer.OpenBrace();

                if (node.Method.Name == MethodCall.StartsWith || node.Method.Name == MethodCall.Contains)
                {
                    writer.Parameter("%");
                    writer.Delimiter();
                }
            }

            base.Visit(node.Arguments[0]);

            if (settings.Engine == DatabaseEngine.MySQL)
            {
                if (node.Method.Name == MethodCall.EndsWith || node.Method.Name == MethodCall.Contains)
                {
                    writer.Delimiter();
                    writer.Parameter("%");
                }

                writer.CloseBrace();
            }

            return node;
        }

        /// <summary>
        /// System.String 的 IsNullOrEmpty 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitIsEmpty(MethodCallExpression node)
        {
            var objExp = node.Arguments.Count > 0 ? node.Arguments[0] : node.Object;

            writer.OpenBrace();

            base.Visit(objExp);

            writer.IsNull();

            writer.Or();

            base.Visit(objExp);

            writer.Equal();
            writer.EmptyString();
            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// System.String 的 Replace 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitToReplace(MethodCallExpression node)
        {
            writer.Write(node.Method.Name);
            writer.OpenBrace();

            base.Visit(node.Object);

            foreach (Expression item in node.Arguments)
            {
                writer.Delimiter();

                base.Visit(item);
            }

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// System.String 的 Substring 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitToSubstring(MethodCallExpression node)
        {
            writer.Write("CASE WHEN ");

            base.Visit(node.Object);

            writer.Write(" IS NULL OR ");

            writer.OpenBrace();
            writer.LengthMethod();
            writer.OpenBrace();

            base.Visit(node.Object);

            writer.CloseBrace();
            writer.Write(" - ");

            base.Visit(node.Arguments[0]);

            writer.CloseBrace();

            writer.Write(" < 1");

            writer.Write(" THEN ");
            writer.Parameter(string.Empty);
            writer.Write(" ELSE ");

            writer.SubstringMethod();
            writer.OpenBrace();

            base.Visit(node.Object);

            writer.Delimiter();

            if (IsPlainVariable(node.Arguments[0]))
            {
                writer.Parameter((int)node.Arguments[0].GetValueFromExpression() + 1);
            }
            else
            {
                base.Visit(node.Arguments[0]);

                writer.Write(" + 1");
            }

            writer.Delimiter();

            if (node.Arguments.Count > 1)
            {
                base.Visit(node.Arguments[1]);
            }
            else
            {
                writer.LengthMethod();
                writer.OpenBrace();

                base.Visit(node.Object);

                writer.CloseBrace();
                writer.Write(" - ");

                base.Visit(node.Arguments[0]);
            }

            writer.CloseBrace();

            writer.Write(" END");

            return node;
        }

        /// <summary>
        /// System.String 的 ToUpper、ToLower 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitToCaseConversion(MethodCallExpression node)
        {
            writer.Write(node.Method.Name.Substring(2));
            writer.OpenBrace();

            base.Visit(node.Object);

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// System.String 的 Trim、TrimStart、TrimEnd 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitToTrim(MethodCallExpression node)
        {
            if (node.Method.Name == MethodCall.TrimStart || node.Method.Name == MethodCall.Trim)
            {
                writer.Write("LTRIM");
                writer.OpenBrace();
            }

            if (node.Method.Name == MethodCall.TrimEnd || node.Method.Name == MethodCall.Trim)
            {
                writer.Write("RTRIM");
                writer.OpenBrace();
            }

            base.Visit(node.Object);

            if (node.Method.Name == MethodCall.TrimStart || node.Method.Name == MethodCall.Trim)
            {
                writer.CloseBrace();
            }

            if (node.Method.Name == MethodCall.TrimEnd || node.Method.Name == MethodCall.Trim)
            {
                writer.CloseBrace();
            }

            return node;
        }

        /// <summary>
        /// System.String 的 IndexOf(int) 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitByIndexOf(MethodCallExpression node)
        {
            var indexOfExp = node.Arguments[0];

            if (IsPlainVariable(indexOfExp, true))
            {
                var value = indexOfExp.GetValueFromExpression();

                if (value is null)
                {
                    writer.Parameter(-1);

                    return node;
                }

                writer.OpenBrace();

                writer.Write("CASE WHEN ");

                base.Visit(node.Object);
            }
            else
            {
                writer.OpenBrace();

                writer.Write("CASE WHEN ");

                base.Visit(node.Object);

                writer.Write(" IS NULL OR ");

                base.Visit(node.Arguments[0]);
            }

            writer.Write(" IS NULL THEN -1 ELSE ");

            writer.IndexOfMethod();
            writer.OpenBrace();

            base.Visit(settings.IndexOfSwapPlaces ? indexOfExp : node.Object);

            writer.Delimiter();

            base.Visit(settings.IndexOfSwapPlaces ? node.Object : indexOfExp);

            writer.CloseBrace();

            writer.Write(" - 1 END");

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// System.String 的 IndexOf(int,int) 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual Expression VisitByIndexOfWithLimit(MethodCallExpression node)
        {
            var objExp = node.Arguments[1];

            var isVariable = IsPlainVariable(objExp);

            var indexStart = isVariable ? (int)objExp.GetValueFromExpression() : -1;

            writer.Write("CASE WHEN ");

            base.Visit(settings.IndexOfSwapPlaces ? node.Arguments[0] : node.Object);

            writer.Write(" IS NULL OR ");

            base.Visit(settings.IndexOfSwapPlaces ? node.Object : node.Arguments[0]);

            writer.Write(" IS NULL ");

            if (node.Arguments.Count > 2)
            {
                writer.Write(" OR ");
                writer.IndexOfMethod();
                writer.OpenBrace();

                base.Visit(settings.IndexOfSwapPlaces ? node.Arguments[0] : node.Object);

                writer.Delimiter();

                base.Visit(settings.IndexOfSwapPlaces ? node.Object : node.Arguments[0]);

                writer.Delimiter();

                if (isVariable)
                {
                    writer.Parameter(indexStart + 1);
                }
                else
                {
                    base.Visit(objExp);

                    writer.Write(" + 1");
                }

                writer.CloseBrace();

                writer.Write(" > ");

                if (isVariable)
                {
                    writer.Parameter(indexStart);
                }
                else
                {
                    base.Visit(objExp);
                }

                writer.Write(" + ");

                base.Visit(node.Arguments[2]);
            }

            writer.Write(" THEN -1 ELSE ");

            writer.IndexOfMethod();
            writer.OpenBrace();

            base.Visit(settings.IndexOfSwapPlaces ? node.Arguments[0] : node.Object);

            writer.Delimiter();

            base.Visit(settings.IndexOfSwapPlaces ? node.Object : node.Arguments[0]);

            writer.Delimiter();

            if (isVariable)
            {
                writer.Parameter(indexStart + 1);
            }
            else
            {
                base.Visit(objExp);

                writer.Write(" + 1");
            }

            writer.CloseBrace();

            writer.Write(" - 1 END");

            return node;
        }

        /// <summary>
        /// 自定义函数支持。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitByCustom(MethodCallExpression node)
        {
            var visitors = GetCustomVisitors();

            foreach (var item in visitors ?? Empty)
            {
                if (item.CanResolve(node)) return item.Visit(this, writer, node);
            }

            foreach (var item in settings.Visitors ?? Empty)
            {
                if (item.CanResolve(node)) return item.Visit(this, writer, node);
            }

            var declaringType = node.Method.DeclaringType;

            throw new DSyntaxErrorException($"命名空间({declaringType.Namespace})下的类({declaringType.Name})中的方法({node.Method.Name})不被支持!");
        }

        /// <summary>
        /// System.Linq.Enumerable 的Union/Concat/Except/Intersect函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitCombination(MethodCallExpression node)
        {
            if (isInTheCondition)
            {
                writer.OpenBrace();
            }

            using (var visitor = new CombinationVisitor(this))
            {
                visitor.Startup(node);
            }

            if (isInTheCondition)
            {
                writer.CloseBrace();
            }

            return node;
        }

        /// <summary>
        /// 条件。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitCondition(MethodCallExpression node)
        {
            base.Visit(node.Arguments[0]);

            Workflow(whereIsNotEmpty =>
            {
                if (whereIsNotEmpty)
                {
                    whereSwitch.Execute();
                }

            }, () =>
            {
                isInTheCondition = true;

                VisitBinaryIsConditionToVisit(node.Arguments[1]);

                isInTheCondition = false;
            });

            return node;
        }

        /// <summary>
        /// 条件。
        /// </summary>
        /// <param name="whereCore">条件表达式分析部分。</param>
        /// <param name="whereIf">表达式分析结果是否不为空。</param>
        /// <returns></returns>
        protected Expression UsingCondition(Func<Expression> whereCore, Action<bool> whereIf = null)
        {
            Expression node = null;

            Workflow(whereIsNotEmpty => whereIf?.Invoke(whereIsNotEmpty), () =>
            {
                isInTheCondition = true;

                node = whereCore.Invoke();

                isInTheCondition = false;
            });

            return node;
        }

        /// <summary>
        /// 包装表达式
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
            {
                return InvertWhere(() =>
                {
                    return base.VisitUnary(node);
                });
            }

            return base.VisitUnary(node);
        }

        /// <summary>
        /// 条件是变量。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitConditionalIsVariable(ConditionalExpression node)
        {
            if (Equals(node.Test.GetValueFromExpression(), true))
            {
                return Visit(node.IfTrue);
            }
            else
            {
                return Visit(node.IfFalse);
            }
        }

        /// <inheritdoc />
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            if (IsPlainVariable(node.Test, true))
            {
                return VisitConditionalIsVariable(node);
            }

            writer.OpenBrace();
            writer.Write("CASE WHEN ");

            Visit(node.Test);

            writer.Write(" THEN ");

            Visit(node.IfTrue);

            writer.Write(" ELSE ");

            Visit(node.IfFalse);

            writer.Write(" END");
            writer.CloseBrace();

            return node;
        }

        /// <inheritdoc />
        protected override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            writer.Write(" WHEN ");

            node.TestValues.ForEach((item, index) =>
            {
                if (index > 0)
                {
                    writer.Or();
                }

                writer.OpenBrace();

                base.Visit(item);

                writer.CloseBrace();
            });

            writer.Write(" THEN ");

            base.Visit(node.Body);

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitSwitch(SwitchExpression node)
        {
            writer.Write("CASE ");

            base.Visit(node.SwitchValue);

            node.Cases.ForEach(item =>
            {
                VisitSwitchCase(item);
            });

            writer.Write(" ELSE ");

            base.Visit(node.DefaultBody);

            writer.Write(" END");

            return node;
        }

        /// <summary>
        /// 是条件(condition1 And condition2)。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitBinaryIsCondition(BinaryExpression node, bool isAndLike)
        {
            int indexBefore = writer.Length;

            VisitBinaryIsConditionToVisit(node.Left);

            int indexStep = writer.Length;

            VisitBinaryIsConditionToVisit(node.Right);

            if (indexStep > indexBefore && writer.Length > indexStep)
            {
                int index = writer.Length;

                int length = writer.Length;

                int appendAt = writer.AppendAt;

                if (appendAt > -1)
                {
                    indexStep -= (index - appendAt);

                    indexBefore -= (index - appendAt);
                }

                writer.AppendAt = indexBefore;

                writer.OpenBrace();

                writer.AppendAt = indexStep + 1;

                if (isAndLike)
                {
                    writer.And();
                }
                else
                {
                    writer.Or();
                }

                if (appendAt > -1)
                {
                    appendAt += writer.Length - length;
                }

                writer.AppendAt = appendAt;

                writer.CloseBrace();
            }

            return node;
        }

        /// <summary>
        /// 空适配符。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitBinaryIsCoalesce(BinaryExpression node)
        {
            if (IsPlainVariable(node.Left, true))
            {
                int length = writer.Length;

                ignoreNullable = true;

                VisitCheckIfSubconnection(node.Left);

                ignoreNullable = false;

                if (writer.Length == length)
                {
                    VisitCheckIfSubconnection(node.Right);
                }

                return node;
            }

            writer.OpenBrace();

            writer.Write("CASE WHEN ");

            VisitCheckIfSubconnection(node.Left);

            writer.IsNull();
            writer.Write(" THEN ");

            VisitCheckIfSubconnection(node.Right);

            writer.Write(" ELSE ");

            VisitCheckIfSubconnection(node.Left);

            writer.Write(" END");

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// 拼接。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitBinaryIsAdd(BinaryExpression node)
        {
            bool useConcat = (settings.Engine == DatabaseEngine.MySQL || settings.Engine == DatabaseEngine.Oracle) && node.Type == typeof(string);

            int indexBefore = writer.Length;

            VisitCheckIfSubconnection(node.Right);

            int indexStep = writer.Length;

            if (indexStep == indexBefore)
            {
                return node;
            }

            int index = writer.Length;

            int length = writer.Length;

            int appendAt = writer.AppendAt;

            if (appendAt > -1)
            {
                indexBefore -= index - appendAt;
            }

            writer.AppendAt = indexBefore;

            if (useConcat)
            {
                writer.Write("CONCAT");
            }

            writer.OpenBrace();

            int indexNext = writer.Length;

            VisitCheckIfSubconnection(node.Left);

            if (writer.Length == indexNext)
            {
                writer.Remove(indexBefore, indexNext - indexBefore);

                writer.AppendAt = appendAt;

                return node;
            }

            if (useConcat)
            {
                writer.Delimiter();
            }
            else
            {
                writer.Write(node.NodeType);
            }

            if (appendAt > -1)
            {
                appendAt += writer.Length - length;
            }

            writer.AppendAt = appendAt;

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// 单个条件(field=1)。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitBinaryIsBoolean(BinaryExpression node)
        {
            int indexBefore = writer.Length;

            ignoreNullable = node.Left.Type.IsNullable();

            VisitCheckIfSubconnection(node.Right);

            ignoreNullable = false;

            int indexStep = writer.Length;

            if (indexStep == indexBefore)
            {
                return node;
            }

            int index = writer.Length;

            int length = writer.Length;

            int appendAt = writer.AppendAt;

            if (appendAt > -1)
            {
                indexBefore -= index - appendAt;
            }

            writer.AppendAt = indexBefore;

            writer.OpenBrace();

            int indexNext = writer.Length;

            ignoreNullable = node.Left.Type.IsNullable();

            VisitCheckIfSubconnection(node.Left);

            ignoreNullable = false;

            if (writer.Length == indexNext)
            {
                writer.Remove(indexBefore, indexNext - indexBefore);

                writer.AppendAt = appendAt;

                return node;
            }

            writer.Write(node.NodeType);

            if (appendAt > -1)
            {
                appendAt += writer.Length - length;
            }

            writer.AppendAt = appendAt;

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// 位运算(field&amp;1)。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitBinaryIsBit(BinaryExpression node)
        {
            int indexBefore = writer.Length;

            ignoreNullable = true;

            Visit(node.Right);

            ignoreNullable = false;

            int indexStep = writer.Length;

            if (indexStep == indexBefore)
            {
                return node;
            }

            int index = writer.Length;

            int length = writer.Length;

            int appendAt = writer.AppendAt;

            if (appendAt > -1)
            {
                indexBefore -= index - appendAt;
            }

            writer.AppendAt = indexBefore;

            writer.OpenBrace();

            int indexNext = writer.Length;

            ignoreNullable = true;

            Visit(node.Left);

            ignoreNullable = false;

            if (writer.Length == indexNext)
            {
                writer.Remove(indexBefore, indexNext - indexBefore);

                writer.AppendAt = appendAt;

                return node;
            }

            writer.Write(node.NodeType);

            if (appendAt > -1)
            {
                appendAt += writer.Length - length;
            }

            writer.AppendAt = appendAt;

            writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// 条件运算（a&amp;&amp;b）。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitBinaryIsConditionToVisit(Expression node)
        {
            if (!node.Type.IsBoolean())
            {
                return VisitCheckIfSubconnection(node);
            }

            if (node.NodeType == ExpressionType.MemberAccess || IsPlainVariable(node))
            {
                try
                {
                    isConditionBalance = true;

                    return VisitCheckIfSubconnection(node);
                }
                finally
                {
                    isConditionBalance = false;
                }
            }

            return VisitCheckIfSubconnection(node);
        }

        /// <summary>
        /// 判断是否为子连接。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitCheckIfSubconnection(Expression node)
        {
            if (node.NodeType == ExpressionType.Call && (node is MethodCallExpression callExpression))
            {
                if (callExpression.Method.Name == MethodCall.Any || callExpression.Method.Name == MethodCall.All || callExpression.Method.Name == MethodCall.Contains)
                {
                    return Visit(node);
                }

                if (callExpression.Method.DeclaringType == typeof(Queryable))
                {
                    writer.OpenBrace();

                    using (var visitor = new SelectVisitor(this))
                    {
                        visitor.Startup(node);
                    }

                    writer.CloseBrace();

                    return node;
                }
            }

            return Visit(node);
        }

        /// <inheritdoc />
        protected override Expression VisitBinary(BinaryExpression node)
        {
            Expression left = node.Left;
            Expression right = node.Right;

            if (node.NodeType == ExpressionType.Coalesce)
            {
                return VisitBinaryIsCoalesce(node);
            }

            var nodeType = node.NodeType;

            bool isAndLike = nodeType == ExpressionType.AndAlso;
            bool isOrLike = nodeType == ExpressionType.OrElse;

            bool isAndOrLike = isAndLike || isOrLike;

            if (isAndOrLike && left.Type.IsBoolean() && IsPlainVariable(left, true))
            {
                if (writer.ReverseCondition)
                {
                    isAndLike ^= isOrLike;
                    isOrLike ^= isAndLike;
                    isAndLike ^= isOrLike;
                }

                var value = left.GetValueFromExpression();

                if (value is null || value.Equals(false))
                {
                    if (isOrLike)
                    {
                        VisitBinaryIsConditionToVisit(right);
                    }
                }
                else if (isAndLike)
                {
                    VisitBinaryIsConditionToVisit(right);
                }

                return node;
            }

            if (isAndOrLike)
            {
                return VisitBinaryIsCondition(node, isAndLike);
            }

            if (nodeType == ExpressionType.Add || nodeType == ExpressionType.AddChecked)
            {
                return VisitBinaryIsAdd(node);
            }

            if (node.Type.IsBoolean())
            {
                return VisitBinaryIsBoolean(node);
            }

            return VisitBinaryIsBit(node);
        }

        #region SQL
        /// <summary>
        /// 生成SQL语句。
        /// </summary>
        /// <returns></returns>
        public virtual string ToSQL() => writer.ToSQL();
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        /// <inheritdoc />
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }

                disposedValue = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
        }
        #endregion
        #endregion
    }
}

using CodeArts.ORM.Exceptions;
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

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// 基础访问器。
    /// </summary>
    public abstract class BaseVisitor : ExpressionVisitor, IDisposable
    {
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
        private static readonly List<ICustomVisitor> Empty = new List<ICustomVisitor>();

        /// <summary>
        /// 实体类型。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Type> EntryCache = new ConcurrentDictionary<Type, Type>();

        /// <summary>
        /// 表别名。
        /// </summary>
        internal readonly ConcurrentDictionary<Type, string> AliasCache = new ConcurrentDictionary<Type, string>();

        /// <inheritdoc />
        protected BaseVisitor(ISQLCorrectSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            this.writer = CreateWriter(settings, CreateWriterMap(settings), new Dictionary<string, object>());
        }

        /// <inheritdoc />
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
        }

        /// <summary>
        /// 能解决的表达式。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public virtual bool CanResolve(MethodCallExpression node) => true;

        private static readonly Type BaseVisitorType = typeof(BaseVisitor);

        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<MemberInfo, string>> MemberNamingCache = new ConcurrentDictionary<Type, ConcurrentDictionary<MemberInfo, string>>();

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
        /// <param name="node">启动节点。</param>
        /// <returns></returns>
        protected virtual Expression StartupCore(MethodCallExpression node)
        {
            return base.Visit(node);
        }

        /// <summary>
        /// 创建写入映射关系。
        /// </summary>
        /// <param name="settings">修正配置。</param>
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
        protected virtual IEnumerable<ICustomVisitor> GetCustomVisitors() => visitor?.GetCustomVisitors() ?? Empty;

        #region 辅佐。
        /// <inheritdoc />
        protected ITableInfo MakeTableInfo(Type paramererType)
        {
            Type entryType = TypeToUltimateType(paramererType);

            if (_CurrentRegions is null)
            {
                return _CurrentRegions = TableRegions.Resolve(entryType);
            }

            return _CurrentRegions.TableType == entryType ? _CurrentRegions : _CurrentRegions = TableRegions.Resolve(entryType);
        }


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
        protected virtual bool TryGetEntryAlias(Type entryType, string parameterName, bool check, out string aliasName)
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
        /// 绑定成员。
        /// </summary>
        /// <param name="bindings">成员集合。</param>
        /// <returns></returns>
#if NET40
        protected virtual ReadOnlyCollection<MemberBinding> FilterMemberBindings(ReadOnlyCollection<MemberBinding> bindings) => bindings;
#else

        protected virtual IReadOnlyCollection<MemberBinding> FilterMemberBindings(IReadOnlyCollection<MemberBinding> bindings) => bindings;
#endif

        /// <summary>
        /// 变量成员。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitMemberIsVariable(MemberExpression node)
        {
            var value = node.GetValueFromExpression();

            if (value is IQueryable queryable)
            {
                base.Visit(queryable.Expression);

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
            writer.NameDot(prefix, field);
        }

        /// <summary>
        /// 写入指定成员。
        /// </summary>
        /// <param name="prefix">前缀。</param>
        /// <param name="members">成员集合。</param>
        protected virtual void WriteMembers(string prefix, IEnumerable<KeyValuePair<string, string>> members)
        {
            var enumerator = members.GetEnumerator();

            if (enumerator.MoveNext())
            {
                do
                {
                    WriteMember(prefix, enumerator.Current.Value, enumerator.Current.Key);

                    if (enumerator.MoveNext())
                    {
                        writer.Delimiter();

                        continue;
                    }

                    break;

                } while (true);
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
        /// 叶子参数依赖于对象。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitParameterLeavesIsObject(ParameterExpression node)
        {
            var regions = MakeTableInfo(node.Type);

            var prefix = GetEntryAlias(regions.TableType, node.Name);

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

        /// <inheritdoc />
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
        protected override Expression VisitParameter(ParameterExpression node)
        {
            writer.Limit(GetEntryAlias(node.Type, node.Name));

            return node;
        }

        /// <inheritdoc />
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

            if (node.Type.IsQueryable())
            {
                return node;
            }

            writer.Parameter((node.Value as ConstantExpression ?? node).GetValueFromExpression());

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (node.Parameters.Count > 1)
            {
                throw new DSyntaxErrorException("不支持多个参数!");
            }

            var parameter = node.Parameters[0];

            var parameterType = TypeToUltimateType(parameter.Type);

            AliasCache.TryAdd(parameterType, parameter.Name);

            if (node.Body.NodeType == ExpressionType.Parameter)
            {
                VisitParameterLeavesIsObject(parameter);
            }
            else
            {
                base.Visit(node.Body);
            }
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

            if (declaringType == typeof(RepositoryExtentions))
            {
                return VisitOfSelect(node);
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
            using (var visitor = new AnyVisitor(this))
            {
                return visitor.Startup(node);
            };
        }

        /// <summary>
        ///  System.Linq.Queryable 的All函数。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        protected virtual Expression VisitOfQueryableAll(MethodCallExpression node)
        {
            using (var visitor = new AllVisitor(this))
            {
                return visitor.Startup(node);
            };
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

                        return base.Visit(node.Arguments[0]);
                    }

                    throw new NotSupportedException("函数“From”的参数类型必须是“System.Func<ITableInfo, string>”委托!");
                default:
                    throw new NotSupportedException($"类型“{node.Method.DeclaringType}”的函数“{node.Method.Name}”不被支持!");
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
                    return member.Expression is null || IsPlainVariable(member.Expression, depthVerification);
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
        /// 自定义函数支持。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual Expression VisitByCustom(MethodCallExpression node)
        {
            var visitors = GetCustomVisitors();

            foreach (var visitor in visitors ?? Empty)
            {
                if (visitor.CanResolve(node)) return visitor.Visit(this, writer, node);
            }

            foreach (var visitor in settings.Visitors ?? Empty)
            {
                if (visitor.CanResolve(node)) return visitor.Visit(this, writer, node);
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
            using (var visitor = new CombinationVisitor(this))
            {
                return visitor.Startup(node);
            }
        }

        /// <inheritdoc />
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
            {
                return writer.ReverseCondition(() => base.VisitUnary(node));
            }

            return base.VisitUnary(node);
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
        /// 表名称。
        /// </summary>
        /// <param name="tableInfo">表信息。</param>
        /// <returns></returns>
        protected string GetTableName(ITableInfo tableInfo) => tableGetter?.Invoke(tableInfo) ?? tableInfo.TableName;

        /// <summary>
        /// 获取成员命名。
        /// </summary>
        /// <param name="memberOfHostType">成员所在类型。</param>
        /// <param name="memberInfo">成员。</param>
        /// <returns></returns>
        protected virtual string GetMemberNaming(Type memberOfHostType, MemberInfo memberInfo)
        => MemberNamingCache
            .GetOrAdd(memberOfHostType, _ => new ConcurrentDictionary<MemberInfo, string>())
            .GetOrAdd(memberInfo, _ =>
            {
                var tableInfo = TableRegions.Resolve(memberOfHostType);

                if (!tableInfo.ReadOrWrites.TryGetValue(memberInfo.Name, out string value))
                {
                    throw new DSyntaxErrorException($"“{memberInfo.Name}”不可读写!");
                }

                return value;
            });

        #region SQL
        /// <summary>
        /// 参数集合。
        /// </summary>
        public Dictionary<string, object> Parameters => writer.Parameters;

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

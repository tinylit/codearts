using CodeArts.Db.Exceptions;
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

        private readonly bool hasVisitor = false;

        private readonly bool isMainVisitor = false;

        private bool buildFrom = false;

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
        private readonly Dictionary<Type, string> AliasCache = new Dictionary<Type, string>();

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

            hasVisitor = true;

            isMainVisitor = this is SelectVisitor || this is JoinVisitor || this is UpdateVisitor || this is InsertVisitor || this is DeleteVisitor;

            this.isNewWriter = isNewWriter;

            var writer = visitor.writer;

            settings = visitor.settings;

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
        /// <param name="node">节点。</param>
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
        public virtual void Startup(Expression node)
        {
            try
            {
                switch (node)
                {
                    case MethodCallExpression callExpression:
                        if (CanResolve(callExpression))
                        {
                            StartupCore(callExpression);

                            break;
                        }

                        throw new NotSupportedException();
                    default:
                        if (CanResolveCache.GetOrAdd(GetType(), _ =>
                        {
                            var method = GetMethodInfo(CanResolve);

                            return method.DeclaringType == BaseVisitorType;
                        }))
                        {
                            Visit(node);

                            break;
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
        protected virtual void StartupCore(MethodCallExpression node) => VisitMethodCall(node);

        /// <summary>
        /// 创建写入映射关系。
        /// </summary>
        /// <param name="settings">修正配置。</param>
        /// <returns></returns>
        protected virtual IWriterMap CreateWriterMap(ISQLCorrectSettings settings) => hasVisitor ? visitor.CreateWriterMap(settings) : new WriterMap(settings);

        /// <summary>
        /// 创建写入流。
        /// </summary>
        /// <param name="settings">修正配置。</param>
        /// <param name="writeMap">写入映射关系。</param>
        /// <param name="parameters">参数。</param>
        /// <returns></returns>
        protected virtual Writer CreateWriter(ISQLCorrectSettings settings, IWriterMap writeMap, Dictionary<string, object> parameters) => hasVisitor ? visitor.CreateWriter(settings, writeMap, parameters) : new Writer(settings, writeMap, parameters);

        /// <inheritdoc />
        protected virtual IEnumerable<ICustomVisitor> GetCustomVisitors() => hasVisitor ? visitor.GetCustomVisitors() : Empty;

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
        /// 表名称。
        /// </summary>
        /// <param name="tableInfo">表信息。</param>
        /// <returns></returns>
        private string GetTableName(ITableInfo tableInfo)
        {
            if (!hasVisitor || isMainVisitor)
            {
                return tableGetter?.Invoke(tableInfo) ?? tableInfo.TableName;
            }

            return visitor.GetTableName(tableInfo);
        }

        /// <summary>
        /// 写入表名称。
        /// </summary>
        /// <param name="paramererType">参数类型。</param>
        protected virtual void WriteTableName(Type paramererType)
        {
            if (!hasVisitor || isMainVisitor)
            {
                var tableInfo = MakeTableInfo(paramererType);

                WriteTableName(tableInfo, GetEntryAlias(tableInfo.TableType, string.Empty));
            }
            else
            {
                visitor.WriteTableName(paramererType);
            }
        }

        /// <summary>
        /// 写入表名称。
        /// </summary>
        /// <param name="tableInfo">表信息。</param>
        /// <param name="alias">表别名。</param>
        protected virtual void WriteTableName(ITableInfo tableInfo, string alias)
        {
            if (!hasVisitor || isMainVisitor)
            {
                string tableName = GetTableName(tableInfo);

                writer.NameWhiteSpace(tableName, alias);
            }
            else
            {
                visitor.WriteTableName(tableInfo, alias);
            }
        }

        /// <summary>
        /// 获取最根本的类型。
        /// </summary>
        /// <returns></returns>
        protected virtual Type TypeToUltimateType(Type entryType)
        {
            if (!hasVisitor || isMainVisitor)
            {
                return TypeToEntryType(entryType, true);
            }

            return visitor.TypeToUltimateType(entryType);
        }

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
        protected internal virtual Type TypeToEntryType(Type repositoryType, bool throwsError = true)
        {
            return EntryCache.GetOrAdd(repositoryType, type => hasVisitor ? visitor.TypeToEntryTypeLogic(type, false) ?? TypeToEntryTypeLogic(type, throwsError) : TypeToEntryTypeLogic(type, throwsError));
        }

        /// <summary>
        /// 获取真实表类型逻辑实现。
        /// </summary>
        /// <param name="repositoryType">类型。</param>
        /// <param name="throwsError">不符合表类型时，是否抛出异常。</param>
        /// <returns></returns>
        protected virtual Type TypeToEntryTypeLogic(Type repositoryType, bool throwsError = true)
        {
            Type baseType = repositoryType;

            while (baseType.IsQueryable())
            {
                if (baseType.IsGenericType)
                {
                    foreach (Type type in baseType.GetGenericArguments())
                    {
                        if (type.IsValueType || type == typeof(string))
                        {
                            continue;
                        }

                        if (type.IsClass || type.IsGrouping())
                        {
                            return type;
                        }
                    }
                }

                baseType = baseType.BaseType;
            };

            if (baseType.IsGrouping())
            {
                return baseType;
            }

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

            return baseType;
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

            return hasVisitor && visitor.TryGetEntryAlias(entryType, parameterName, check, out aliasName);
        }

        /// <summary>
        /// 获取表别名。
        /// </summary>
        /// <param name="parameterType">表类型。</param>
        /// <param name="parameterName">参数名称。</param>
        /// <returns></returns>
        protected string GetEntryAlias(Type parameterType, string parameterName)
        {
            if (!hasVisitor || isMainVisitor)
            {
                Type entryType = DelegateTypeOrExpressionTypeToEntryType(parameterType);

                if (TryGetEntryAlias(entryType, parameterName, true, out string aliasName) || TryGetEntryAlias(entryType, parameterName, false, out aliasName))
                {
                    return aliasName;
                }

                if (AliasCache.TryGetValue(entryType, out string value))
                {
                    return value;
                }

                AliasCache.Add(entryType, parameterName);

                return parameterName;
            }

            return visitor.GetEntryAlias(parameterType, parameterName);
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
        protected virtual ReadOnlyCollection<MemberBinding> FilterMemberBindings(ReadOnlyCollection<MemberBinding> bindings) => hasVisitor ? visitor.FilterMemberBindings(bindings) : bindings;
#else

        protected virtual IReadOnlyCollection<MemberBinding> FilterMemberBindings(IReadOnlyCollection<MemberBinding> bindings) => hasVisitor ? visitor.FilterMemberBindings(bindings) : bindings;
#endif

        /// <summary>
        /// 变量成员。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitMemberIsVariable(MemberExpression node)
        {
            var value = node.GetValueFromExpression();

            if (value is IQueryable queryable)
            {
                Visit(queryable.Expression);

                return;
            }

            if (node.Expression is null || node.Expression.NodeType == ExpressionType.MemberAccess && (node.Type.IsValueType || node.Expression.Type == node.Type))
            {
                writer.Parameter(string.Concat("__variable_", node.Member.Name.ToLower()), value);
            }
            else
            {
                writer.Parameter(node.Member.Name, value);
            }
        }

        /// <summary>
        /// 成员依赖于参数成员（参数类型是对象）。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitMemberIsDependOnParameterTypeIsObject(MemberExpression node)
        {
            if (!hasVisitor || isMainVisitor)
            {
                writer.Limit(GetEntryAlias(node.Type, node.Member.Name));
            }
            else
            {
                visitor.VisitMemberIsDependOnParameterTypeIsObject(node);
            }
        }

        /// <summary>
        /// 成员依赖于参数成员（参数类型是值类型或字符串类型）。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitMemberIsDependOnParameterTypeIsPlain(MemberExpression node)
        {
            if (!hasVisitor || isMainVisitor)
            {
                var tableInfo = MakeTableInfo(node.Expression.Type);

                if (!tableInfo.ReadOrWrites.TryGetValue(node.Member.Name, out string value))
                {
                    throw new DSyntaxErrorException($"“{node.Member.Name}”不可读写!");
                }

                writer.Name(value);
            }
            else
            {
                visitor.VisitMemberIsDependOnParameterTypeIsPlain(node);
            }
        }

        /// <summary>
        /// 过滤成员。
        /// </summary>
        /// <param name="members">成员。</param>
        /// <returns></returns>
#if NET40
        protected virtual IDictionary<string, string> FilterMembers(IDictionary<string, string> members) => hasVisitor ? visitor.FilterMembers(members) : members;
#else
        protected virtual IReadOnlyDictionary<string, string> FilterMembers(IReadOnlyDictionary<string, string> members) => hasVisitor ? visitor.FilterMembers(members) : members;
#endif

        /// <summary>
        /// 过滤成员。
        /// </summary>
        /// <param name="members">成员集合。</param>
        /// <returns></returns>
#if NET40
        protected virtual ReadOnlyCollection<MemberInfo> FilterMembers(ReadOnlyCollection<MemberInfo> members) => hasVisitor ? visitor.FilterMembers(members) : members;
#else
        protected virtual IReadOnlyCollection<MemberInfo> FilterMembers(IReadOnlyCollection<MemberInfo> members) => hasVisitor ? visitor.FilterMembers(members) : members;
#endif

        /// <summary>
        /// 写入成员。
        /// </summary>
        /// <param name="prefix">前缀。</param>
        /// <param name="field">字段。</param>
        protected virtual void WriteMember(string prefix, string field) => writer.NameDot(prefix, field);

        /// <summary>
        /// 写入成员别名。
        /// </summary>
        /// <param name="field">字段。</param>
        /// <param name="alias">别名。</param>
        protected virtual void DefMemberAs(string field, string alias)
        {
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
                    WriteMember(prefix, enumerator.Current.Value);

                    DefMemberAs(enumerator.Current.Value, enumerator.Current.Key);

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
        protected virtual void VisitMemberIsDependOnParameter(MemberExpression node)
        {
            if (node.Type.IsValueType || node.Type == typeof(string))
            {
                VisitMemberIsDependOnParameterTypeIsPlain(node);
            }
            else
            {
                VisitMemberIsDependOnParameterTypeIsObject(node);
            }
        }

        /// <summary>
        /// 叶子成员依赖于对象。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitMemberLeavesIsObject(MemberExpression node)
        {
            if (!hasVisitor || isMainVisitor)
            {
                var regions = MakeTableInfo(node.Type);

                var prefix = GetEntryAlias(regions.TableType, node.Member.Name);

                WriteMembers(prefix, FilterMembers(regions.ReadOrWrites));
            }
            else
            {
                visitor.VisitMemberLeavesIsObject(node);
            }
        }

        /// <summary>
        /// 叶子参数依赖于对象。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitParameterLeavesIsObject(ParameterExpression node)
        {
            if (!hasVisitor || isMainVisitor)
            {
                var regions = MakeTableInfo(node.Type);

                var prefix = GetEntryAlias(regions.TableType, node.Name);

                WriteMembers(prefix, FilterMembers(regions.ReadOrWrites));
            }
            else
            {
                visitor.VisitParameterLeavesIsObject(node);
            }
        }

        /// <summary>
        /// 属性或字段。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitMemberIsPropertyOrField(MemberExpression node) => VisitMemberIsVariable(node);

        /// <inheritdoc />
        protected override Expression VisitMember(MemberExpression node)
        {
            if (IsPlainVariable(node))
            {
                VisitMemberIsVariable(node);

                return node;
            }

            if (node.Expression is null)
            {
                VisitMemberIsPropertyOrField(node);

                return node;
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
                        VisitMember(member);
                    }
                    else if (node.IsHasValue())
                    {
                        try
                        {
                            VisitMember(member);
                        }
                        finally
                        {
                            writer.IsNotNull();
                        }
                    }
                    break;
                case ParameterExpression parameter:

                    if (node.Type.IsValueType || node.Type == typeof(string))
                    {
                        VisitParameter(parameter);

                        VisitMemberIsDependOnParameter(node);

                        break;
                    }

                    VisitMemberLeavesIsObject(node);

                    break;
                case MemberExpression member:
                    VisitMemberIsDependOnParameter(member);
                    goto default;
                default:
                    VisitMemberIsDependOnParameter(node);
                    break;
            }

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (!node.Type.IsGrouping())
            {
                writer.Limit(GetEntryAlias(node.Type, node.Name));
            }

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            Visit(node.Expression);

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.IsBoolean())
            {
                throw new DSyntaxErrorException("禁止使用布尔常量作为条件语句或结果!");
            }

            if (node.Type.IsValueType || node.Type == typeof(string) || node.Type == typeof(Version))
            {
                writer.Parameter((node.Value as ConstantExpression ?? node).GetValueFromExpression());
            }

            if (buildFrom)
            {
                tableGetter = (Func<ITableInfo, string>)(node.Value as ConstantExpression ?? node).GetValueFromExpression();
            }

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (node.Parameters.Count > 1)
            {
                throw new DSyntaxErrorException("不支持多个参数!");
            }

            AnalysisAlias(node.Parameters[0]);

            if (node.Body.NodeType == ExpressionType.Parameter)
            {
                VisitParameterLeavesIsObject(node.Parameters[0]);
            }
            else
            {
                Visit(node.Body);
            }

            return node;
        }

        /// <summary>
        /// 设置表别名。
        /// </summary>
        /// <param name="node">参数节点。</param>
        protected void AnalysisAlias(ParameterExpression node)
        {
            if (!hasVisitor || isMainVisitor)
            {
                var parameterType = TypeToUltimateType(node.Type);

                if (!AliasCache.ContainsKey(parameterType))
                {
                    AliasCache.Add(parameterType, node.Name);
                }
            }
            else
            {
                visitor.AnalysisAlias(node);
            }
        }

        /// <inheritdoc />
        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Quote:

                    Visit(node.Operand);

                    break;
                case ExpressionType.Not when node.Operand is ConstantExpression constant && constant.IsBoolean():

                    writer.Write(node.NodeType);

                    VisitConstant(constant);

                    break;
                case ExpressionType.Not when node.Operand is MemberExpression member && member.IsBoolean() && !member.IsHasValue():

                    writer.Write(node.NodeType);

                    VisitMember(member);

                    break;

                case ExpressionType.Not:

                    writer.ReverseCondition(() => Visit(node.Operand));

                    break;
                case ExpressionType.OnesComplement:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:

                    writer.Write(node.NodeType);

                    Visit(node.Operand);

                    break;

                case ExpressionType.Increment:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.PostIncrementAssign:

                    Visit(node.Operand);

                    writer.Write(ExpressionType.AddChecked);

                    writer.Write("1");

                    break;
                case ExpressionType.Decrement:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PostDecrementAssign:

                    Visit(node.Operand);

                    writer.Write(ExpressionType.SubtractChecked);

                    writer.Write("1");

                    break;

                case ExpressionType.IsTrue:

                    Visit(node.Operand);

                    break;
                case ExpressionType.IsFalse when node.Operand is BinaryExpression binary:

                    writer.ReverseCondition(() => VisitBinary(binary));

                    break;
                case ExpressionType.IsFalse:

                    writer.Write(ExpressionType.Not);

                    Visit(node.Operand);

                    break;
                case ExpressionType.Throw:
                case ExpressionType.TypeAs:
                case ExpressionType.TypeIs:
                case ExpressionType.Convert:
                case ExpressionType.ArrayLength:
                default:

                    base.VisitUnary(node);

                    break;
            }


            return node;
        }
        #region MethodCallExpression
        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var declaringType = node.Method.DeclaringType;

            if (declaringType == Types.Queryable)
            {
                VisitCore(node);
            }
            else if (declaringType == Types.RepositoryExtentions)
            {
                VisitOfLts(node);
            }
            else if (declaringType == Types.String)
            {
                VisitOfString(node);
            }
            else
            {
                VisitByCustom(node);
            }

            return node;
        }

        /// <summary>
        ///  <see cref="Queryable"/>.
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitCore(MethodCallExpression node) => VisitByCustom(node);

        /// <summary>
        /// System.String 的函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual void VisitOfString(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Contains:
                case MethodCall.EndsWith:
                case MethodCall.StartsWith:
                    VisitLike(node);
                    break;
                case MethodCall.IsNullOrEmpty:
                    VisitIsEmpty(node);
                    break;
                case MethodCall.Replace:
                    VisitToReplace(node);
                    break;
                case MethodCall.Substring:
                    VisitToSubstring(node);
                    break;
                case MethodCall.ToUpper:
                case MethodCall.ToLower:
                    VisitToCaseConversion(node);
                    break;
                case MethodCall.Trim:
                case MethodCall.TrimEnd:
                case MethodCall.TrimStart:
                    VisitToTrim(node);
                    break;
                case MethodCall.IndexOf when node.Arguments.Count > 1:
                    VisitByIndexOfWithLimit(node);
                    break;
                case MethodCall.IndexOf:
                    VisitByIndexOf(node);
                    break;
                default:
                    VisitByCustom(node);
                    break;
            }
        }

        #region String

        /// <summary>
        /// System.String 的 Contains、StartsWith、EndsWith函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual void VisitLike(MethodCallExpression node)
        {
            if (node.Arguments.Count > 1)
            {
                throw new DSyntaxErrorException($"仅支持参数类型为“System.String”的({node.Method.Name}(String))方法。");
            }

            if (IsPlainVariable(node.Arguments[0]))
            {
                VisitLikeByVariable(node);
            }
            else
            {
                VisitLikeByExpression(node);
            }
        }

        /// <summary>
        /// System.String 的 Contains、StartsWith、EndsWith函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual void VisitLikeByVariable(MethodCallExpression node)
        {
            var objExp = node.Arguments[0];

            var value = objExp.GetValueFromExpression();

            if (value is null)
            {
                return;
            }

            if (!(value is string text))
            {
                throw new DSyntaxErrorException($"仅支持参数类型为“System.String”的({node.Method.Name})方法。");
            }

            Visit(node.Object);

            if (text.Length == 0)
            {
                writer.IsNotNull();
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
        }

        /// <summary>
        /// System.String 的 Contains、StartsWith、EndsWith函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual void VisitLikeByExpression(MethodCallExpression node)
        {
            Visit(node.Object);

            writer.Like();

            if (settings.Engine == DatabaseEngine.MySQL || settings.Engine == DatabaseEngine.Oracle)
            {
                writer.Write("CONCAT");
                writer.OpenBrace();

                if (node.Method.Name == MethodCall.EndsWith || node.Method.Name == MethodCall.Contains)
                {
                    writer.Write("'%'");
                    writer.Delimiter();
                }
            }
            else if (node.Method.Name == MethodCall.EndsWith || node.Method.Name == MethodCall.Contains)
            {
                writer.Write("'%' + ");
            }

            Visit(node.Arguments[0]);

            if (settings.Engine == DatabaseEngine.MySQL || settings.Engine == DatabaseEngine.Oracle)
            {
                if (node.Method.Name == MethodCall.StartsWith || node.Method.Name == MethodCall.Contains)
                {
                    writer.Delimiter();
                    writer.Write("'%'");
                }

                writer.CloseBrace();
            }
            else if (node.Method.Name == MethodCall.StartsWith || node.Method.Name == MethodCall.Contains)
            {
                writer.Write(" + '%'");
            }
        }

        /// <summary>
        /// System.String 的 IsNullOrEmpty 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual void VisitIsEmpty(MethodCallExpression node)
        {
            var objExp = node.Arguments.Count > 0 ? node.Arguments[0] : node.Object;

            writer.OpenBrace();

            Visit(objExp);

            writer.IsNull();

            writer.Or();

            Visit(objExp);

            writer.Equal();
            writer.EmptyString();
            writer.CloseBrace();
        }

        /// <summary>
        /// System.String 的 Replace 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual void VisitToReplace(MethodCallExpression node)
        {
            writer.Write(node.Method.Name);
            writer.OpenBrace();

            Visit(node.Object);

            foreach (Expression item in node.Arguments)
            {
                writer.Delimiter();

                Visit(item);
            }

            writer.CloseBrace();
        }

        /// <summary>
        /// System.String 的 Substring 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual void VisitToSubstring(MethodCallExpression node)
        {
            writer.Write("CASE WHEN ");

            Visit(node.Object);

            writer.Write(" IS NULL OR ");

            writer.OpenBrace();
            writer.LengthMethod();
            writer.OpenBrace();

            Visit(node.Object);

            writer.CloseBrace();
            writer.Write(" - ");

            Visit(node.Arguments[0]);

            writer.CloseBrace();

            writer.Write(" < 1");

            writer.Write(" THEN ");
            writer.Parameter(string.Empty);
            writer.Write(" ELSE ");

            writer.SubstringMethod();
            writer.OpenBrace();

            Visit(node.Object);

            writer.Delimiter();

            if (IsPlainVariable(node.Arguments[0]))
            {
                writer.Parameter((int)node.Arguments[0].GetValueFromExpression() + 1);
            }
            else
            {
                Visit(node.Arguments[0]);

                writer.Write(" + 1");
            }

            writer.Delimiter();

            if (node.Arguments.Count > 1)
            {
                Visit(node.Arguments[1]);
            }
            else
            {
                writer.LengthMethod();
                writer.OpenBrace();

                Visit(node.Object);

                writer.CloseBrace();
                writer.Write(" - ");

                Visit(node.Arguments[0]);
            }

            writer.CloseBrace();

            writer.Write(" END");
        }

        /// <summary>
        /// System.String 的 ToUpper、ToLower 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual void VisitToCaseConversion(MethodCallExpression node)
        {
#if NETSTANDARD2_1
            writer.Write(node.Method.Name[2..]);
#else
            writer.Write(node.Method.Name.Substring(2));
#endif
            writer.OpenBrace();

            Visit(node.Object);

            writer.CloseBrace();
        }

        /// <summary>
        /// System.String 的 Trim、TrimStart、TrimEnd 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual void VisitToTrim(MethodCallExpression node)
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

            Visit(node.Object);

            if (node.Method.Name == MethodCall.TrimStart || node.Method.Name == MethodCall.Trim)
            {
                writer.CloseBrace();
            }

            if (node.Method.Name == MethodCall.TrimEnd || node.Method.Name == MethodCall.Trim)
            {
                writer.CloseBrace();
            }
        }

        /// <summary>
        /// System.String 的 IndexOf(int) 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual void VisitByIndexOf(MethodCallExpression node)
        {
            var indexOfExp = node.Arguments[0];

            if (IsPlainVariable(indexOfExp, true))
            {
                var value = indexOfExp.GetValueFromExpression();

                if (value is null)
                {
                    writer.Parameter(-1);

                    return;
                }

                writer.OpenBrace();

                writer.Write("CASE WHEN ");

                Visit(node.Object);
            }
            else
            {
                writer.OpenBrace();

                writer.Write("CASE WHEN ");

                Visit(node.Object);

                writer.Write(" IS NULL OR ");

                Visit(node.Arguments[0]);
            }

            writer.Write(" IS NULL THEN -1 ELSE ");

            writer.IndexOfMethod();
            writer.OpenBrace();

            Visit(settings.IndexOfSwapPlaces ? indexOfExp : node.Object);

            writer.Delimiter();

            Visit(settings.IndexOfSwapPlaces ? node.Object : indexOfExp);

            writer.CloseBrace();

            writer.Write(" - 1 END");

            writer.CloseBrace();
        }

        /// <summary>
        /// System.String 的 IndexOf(int,int) 函数。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected virtual void VisitByIndexOfWithLimit(MethodCallExpression node)
        {
            var objExp = node.Arguments[1];

            var isVariable = IsPlainVariable(objExp);

            var indexStart = isVariable ? (int)objExp.GetValueFromExpression() : -1;

            writer.Write("CASE WHEN ");

            Visit(settings.IndexOfSwapPlaces ? node.Arguments[0] : node.Object);

            writer.Write(" IS NULL OR ");

            Visit(settings.IndexOfSwapPlaces ? node.Object : node.Arguments[0]);

            writer.Write(" IS NULL ");

            if (node.Arguments.Count > 2)
            {
                writer.Write(" OR ");
                writer.IndexOfMethod();
                writer.OpenBrace();

                Visit(settings.IndexOfSwapPlaces ? node.Arguments[0] : node.Object);

                writer.Delimiter();

                Visit(settings.IndexOfSwapPlaces ? node.Object : node.Arguments[0]);

                writer.Delimiter();

                if (isVariable)
                {
                    writer.Parameter(indexStart + 1);
                }
                else
                {
                    Visit(objExp);

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
                    Visit(objExp);
                }

                writer.Write(" + ");

                Visit(node.Arguments[2]);
            }

            writer.Write(" THEN -1 ELSE ");

            writer.IndexOfMethod();
            writer.OpenBrace();

            Visit(settings.IndexOfSwapPlaces ? node.Arguments[0] : node.Object);

            writer.Delimiter();

            Visit(settings.IndexOfSwapPlaces ? node.Object : node.Arguments[0]);

            writer.Delimiter();

            if (isVariable)
            {
                writer.Parameter(indexStart + 1);
            }
            else
            {
                Visit(objExp);

                writer.Write(" + 1");
            }

            writer.CloseBrace();

            writer.Write(" - 1 END");
        }
        #endregion

        /// <summary>
        /// <see cref="RepositoryExtentions.From{TSource}(IRepository{TSource}, Func{ITableInfo, string})"/>
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitOfLts(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.From:
                    buildFrom = true;

                    Visit(node.Arguments[1]);

                    buildFrom = false;

                    Visit(node.Arguments[0]);

                    break;
                default:
                    VisitByCustom(node);
                    break;
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
                case MethodCallExpression method when method.Object is null ? !(method.Method.DeclaringType == Types.Queryable || method.Method.DeclaringType == Types.RepositoryExtentions) : IsPlainVariable(method.Object, depthVerification):
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
        protected virtual void VisitByCustom(MethodCallExpression node)
        {
            if (node.Method.Name == "Equals" && node.Arguments.Count == 1)
            {
                var argNode = node.Arguments[0];

                if (node.Object.Type == argNode.Type)
                {
                    Visit(node.Object);

                    writer.Equal();

                    Visit(argNode);

                    return;
                }
            }

            var visitors = GetCustomVisitors();

            foreach (var visitor in visitors ?? Empty)
            {
                if (visitor.CanResolve(node))
                {
                    visitor.Visit(this, writer, node);

                    return;
                }
            }

            var declaringType = node.Method.DeclaringType;

            throw new DSyntaxErrorException($"命名空间({declaringType.Namespace})下的类({declaringType.Name})中的方法({node.Method.Name})不被支持!");
        }

        /// <inheritdoc />
        protected override Expression VisitSwitch(SwitchExpression node)
        {
            writer.Write("CASE ");

            base.Visit(node.SwitchValue);

            foreach (var item in node.Cases)
            {
                VisitSwitchCase(item);
            }

            writer.Write(" ELSE ");

            base.Visit(node.DefaultBody);

            writer.Write(" END");

            return node;
        }

        /// <inheritdoc />
        protected override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            writer.Write(" WHEN ");

            bool flag = false;

            foreach (var item in node.TestValues)
            {
                if (flag)
                {
                    writer.Or();
                }
                else
                {
                    flag = true;
                }

                writer.OpenBrace();

                VisitTail(item);

                writer.CloseBrace();
            }

            writer.Write(" THEN ");

            VisitTail(node.Body);

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            if (IsPlainVariable(node.Test, true))
            {
                if (Equals(node.Test.GetValueFromExpression(), true))
                {
                    VisitTail(node.IfTrue);
                }
                else
                {
                    VisitTail(node.IfFalse);
                }

                return node;
            }

            writer.Write("CASE WHEN ");

            VisitTail(node.Test);

            writer.Write(" THEN ");

            VisitTail(node.IfTrue);

            writer.Write(" ELSE ");

            VisitTail(node.IfFalse);

            writer.Write(" END");

            return node;
        }

        #region 条件

        /// <inheritdoc />
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Coalesce)
            {
                VisitBinaryIsCoalesce(node);

                return node;
            }

            Expression left = node.Left;
            Expression right = node.Right;

            var nodeType = node.NodeType;

            bool isAndLike = nodeType == ExpressionType.AndAlso;
            bool isOrLike = nodeType == ExpressionType.OrElse;

            bool isAndOrLike = isAndLike || isOrLike;

            if (isAndOrLike && left.Type.IsBoolean() && IsPlainVariable(left, true))
            {
                if (writer.IsReverseCondition)
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
                VisitBinaryIsCondition(node, isAndLike);
            }
            else if (nodeType == ExpressionType.Add || nodeType == ExpressionType.AddChecked)
            {
                VisitBinaryIsAdd(node);
            }
            else if (nodeType == ExpressionType.And || nodeType == ExpressionType.Or)
            {
                VisitBinaryIsBoolean(node);
            }
            else if (node.Type.IsBoolean())
            {
                VisitBinaryIsBoolean(node);
            }
            else
            {
                VisitBinaryIsBit(node);
            }

            return node;
        }

        /// <summary>
        /// 是条件(condition1 And condition2)。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitBinaryIsCondition(BinaryExpression node, bool isAndLike)
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
        }

        /// <summary>
        /// 尾巴。
        /// </summary>
        protected virtual void VisitTail(Expression node)
        {
            switch (node)
            {
                case MethodCallExpression method when method.Method.DeclaringType == Types.Queryable:
                    switch (method.Method.Name)
                    {
                        case MethodCall.Any:
                            using (var visitor = new NestedAnyVisitor(this))
                            {
                                visitor.Startup(node);
                            }
                            break;
                        case MethodCall.All:
                            using (var visitor = new NestedAllVisitor(this))
                            {
                                visitor.Startup(node);
                            }
                            break;
                        case MethodCall.Contains:
                            using (var visitor = new NestedContainsVisitor(this))
                            {
                                visitor.Startup(node);
                            }
                            break;
                        default:
                            using (var visitor = new SelectVisitor(this))
                            {
                                writer.OpenBrace();
                                visitor.Startup(node);
                                writer.CloseBrace();
                            }
                            break;
                    }
                    break;
                default:
                    Visit(node);
                    break;
            }
        }

        /// <summary>
        /// 空适配符。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitBinaryIsCoalesce(BinaryExpression node)
        {
            if (IsPlainVariable(node.Left, true))
            {
                var nodeValue = node.Left.GetValueFromExpression();

                if (nodeValue is null)
                {
                    VisitTail(node.Right);

                    return;
                }

                if (node.Left is MemberExpression memberExp)
                {
                    writer.Parameter(memberExp.Member.Name, nodeValue);
                }
                else
                {
                    writer.Parameter(nodeValue);
                }

                return;
            }

            writer.OpenBrace();

            writer.Write("CASE WHEN ");

            VisitTail(node.Left);

            writer.IsNull();
            writer.Write(" THEN ");

            VisitTail(node.Right);

            writer.Write(" ELSE ");

            VisitTail(node.Left);

            writer.Write(" END");

            writer.CloseBrace();
        }

        /// <summary>
        /// 拼接。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitBinaryIsAdd(BinaryExpression node)
        {
            bool useConcat = (settings.Engine == DatabaseEngine.MySQL || settings.Engine == DatabaseEngine.Oracle) && node.Type == typeof(string);

            int indexBefore = writer.Length;

            VisitTail(node.Right);

            int indexStep = writer.Length;

            if (indexStep == indexBefore)
            {
                return;
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

            VisitTail(node.Left);

            if (writer.Length == indexNext)
            {
                writer.Remove(indexBefore, indexNext - indexBefore);

                writer.AppendAt = appendAt;

                return;
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
        }

        /// <summary>
        /// 单个条件(field=1)。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitBinaryIsBoolean(BinaryExpression node)
        {
            int indexBefore = writer.Length;

            VisitBinaryIsIsBooleanRight(node.Right);

            int indexStep = writer.Length;

            if (indexStep == indexBefore)
            {
                return;
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

            VisitBinaryIsBooleanLeft(node.Left);

            if (writer.Length == indexNext)
            {
                writer.Remove(indexBefore, indexNext - indexBefore);

                writer.AppendAt = appendAt;

                return;
            }

            writer.Write(node.NodeType);

            if (appendAt > -1)
            {
                appendAt += writer.Length - length;
            }

            writer.AppendAt = appendAt;

            writer.CloseBrace();
        }

        /// <summary>
        /// 表达式的节点。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void VisitBinaryIsBooleanLeft(Expression node) => VisitTail(node);

        /// <summary>
        /// 表达式的右节点。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void VisitBinaryIsIsBooleanRight(Expression node) => VisitTail(node);

        /// <summary>
        /// 位运算(field&amp;1)。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitBinaryIsBit(BinaryExpression node)
        {
            int indexBefore = writer.Length;

            VisitBinaryIsBit(node.Right);

            int indexStep = writer.Length;

            if (indexStep == indexBefore)
            {
                return;
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

            VisitBinaryIsBit(node.Left);

            if (writer.Length == indexNext)
            {
                writer.Remove(indexBefore, indexNext - indexBefore);

                writer.AppendAt = appendAt;

                return;
            }

            writer.Write(node.NodeType);

            if (appendAt > -1)
            {
                appendAt += writer.Length - length;
            }

            writer.AppendAt = appendAt;

            writer.CloseBrace();
        }

        /// <summary>
        /// 位运算(field&amp;1)。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitBinaryIsBit(Expression node) => VisitTail(node);

        /// <summary>
        /// 条件运算（a&amp;&amp;b）。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitBinaryIsConditionToVisit(Expression node) => Visit(node);
        #endregion

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
                    AliasCache.Clear();

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

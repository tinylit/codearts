using CodeArts.ORM.Exceptions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CodeArts.ORM.Builders
{
    /// <summary>
    /// 构造器
    /// </summary>
    public class QueryBuilder : Builder, IQueryBuilder, IBuilder, IDisposable
    {
        #region 分页

        private int take = -1;

        private int skip = -1;

        private StringBuilder orderby;
        #endregion

        private bool buildUnion = false;

        private bool buildSelect = true;

        private bool buildJoin = false;

        private bool buildFrom = true;

        private bool buildExists = false;

        private bool buildCast = false;

        private bool useCast = false;

        private bool isDistinct = false;

        private bool inSelect = false;

        private bool isAggregation = false; // 聚合函数

        private bool isNoParameterCount = false;//是否为Count函数

        private bool isContainsOrderBy = false; //包含OrderBy

        private bool isOrderByReverse = false; //倒序

        private int _MethodLevel = 0;

        private SmartSwitch _whereSwitch = null;

        private SmartSwitch _orderBySwitch = null;

        private SmartSwitch _fromSwitch = null;

        private List<string> _CastList = null;

        private ConcurrentDictionary<Type, Type> _TypeCache;

        private readonly ISQLCorrectSettings settings;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settings">SQL矫正设置</param>
        public QueryBuilder(ISQLCorrectSettings settings) : base(settings) => this.settings = settings;

        /// <summary>
        /// 表达式测评
        /// </summary>
        /// <param name="node">表达式</param>
        public override void Evaluate(Expression node)
        {
            _whereSwitch = new SmartSwitch(SQLWriter.Where, SQLWriter.WriteAnd);

            _orderBySwitch = new SmartSwitch(SQLWriter.OrderBy, SQLWriter.Delimiter);

            _fromSwitch = new SmartSwitch(SQLWriter.From, SQLWriter.Join);

            _TypeCache = new ConcurrentDictionary<Type, Type>();

            base.Evaluate(node);
        }

        /// <summary>
        /// 内部分析
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="node">节点</param>
        protected override void Evaluate(Writer writer, Expression node)
        {
            var appendAt = writer.AppendAt;

            var index = writer.Length;

            base.Evaluate(writer, node);

            if (take > 0 || skip > 0 || buildUnion && orderby.Length > 0)
            {
                var length = writer.Length - index;

                string value;
                if (appendAt > -1)
                {
                    writer.AppendAt = appendAt;

                    value = writer.ToString(appendAt, length);

                    writer.Remove(appendAt, length);
                }
                else
                {
                    value = writer.ToString(index, length);

                    writer.Remove(index, length);
                }

                writer.Write(ToSQL(value));
            }
        }

        private bool InCastList(string name) => _CastList is null || _CastList.Contains(name.ToLower());

        private Expression VisitExists(MethodCallExpression node)
        {
            SQLWriter.Exists();

            SQLWriter.OpenBrace();

            buildExists = true;

            if (node.Arguments.Count == 1)
            {
                base.Visit(node.Arguments[0]);
            }
            else
            {
                bool whereIsNotEmpty = false;

                WriteAppendAtFix(() =>
                {
                    base.Visit(node.Arguments[0]);

                    if (whereIsNotEmpty)
                    {
                        _whereSwitch.Execute();
                    }
                }, () =>
                {
                    int length = SQLWriter.Length;

                    BuildWhere = true;
                    base.Visit(node.Arguments[1]);
                    BuildWhere = false;

                    whereIsNotEmpty = SQLWriter.Length > length;
                });
            }

            buildExists = false;

            SQLWriter.CloseBrace();

            return node;
        }

        private void VisitMethodAll(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable))
            {
                VisitExists(node);

                SQLWriter.WriteAnd();

                //? 初始参数。
                buildUnion = false;

                buildSelect = true;

                buildJoin = false;

                buildFrom = true;

                buildExists = false;

                buildCast = false;

                useCast = false;

                isDistinct = false;

                inSelect = false;

                isAggregation = false; // 聚合函数

                isNoParameterCount = false;//是否为Count函数

                isContainsOrderBy = false; //包含OrderBy

                isOrderByReverse = false; //倒序

                WrapNot(() => _whereSwitch.UnWrap(() => _fromSwitch.UnWrap(() => _orderBySwitch.UnWrap(() =>
                {
                    VisitExists(node);
                }))));
            }
            else
            {
                throw new DSyntaxErrorException($"仅支持“System.Linq.Queryable”中的{node.Method.Name}函数!");
            }
        }

        private void BuildSingleField(string methodName, string prefix, ITableInfo regions)
        {
            IEnumerable<KeyValuePair<string, string>> names = regions.ReadOrWrites;

            if (buildCast)
            {
                names = names.Where(kv => InCastList(kv.Key));
            }

            if (names.Count() == 0)
            {
                throw new DException("未指定查询字段!");
            }

            names.ForEach((kv, index) =>
            {
                if (index > 0)
                {
                    SQLWriter.Delimiter();
                }

                SQLWriter.Write(methodName);
                SQLWriter.OpenBrace();

                SQLWriter.Name(prefix, kv.Value);

                SQLWriter.CloseBrace();
                SQLWriter.As(kv.Key);
            });
        }

        private Expression BuildSingleOneArgField(string name, Expression node)
        {
            var regions = MakeTableInfo(node.Type);

            string prefix = GetOrAddTablePrefix(regions.TableType);

            BuildSingleField(name, prefix, regions);

            return node;
        }

        private Expression SingleFieldTwoArgOrCountMethod(string name, MethodCallExpression node)
        {
            WriteAppendAtFix(() =>
            {
                SQLWriter.Select();

                SQLWriter.Write(name);
                SQLWriter.OpenBrace();

                if (name == MethodCall.Count || node.Arguments.Count == 1)
                {
                    if (buildJoin || buildUnion)
                    {
                        SQLWriter.Write("1");
                    }
                    else
                    {
                        var tableInfo = MakeTableInfo(node.Arguments[0].Type);

                        if (tableInfo.Keys.Count() == 1)
                        {
                            string prefix = GetOrAddTablePrefix(tableInfo.TableType);

                            WriteMembers(prefix, tableInfo.ReadOrWrites.Where(x => tableInfo.Keys.Contains(x.Key)));
                        }
                        else
                        {
                            SQLWriter.Write("1");
                        }
                    }
                }
                else
                {
                    base.Visit(node.Arguments[1]);
                }

                SQLWriter.CloseBrace();

                if (buildUnion)
                {
                    SQLWriter.From();
                    SQLWriter.OpenBrace();
                }

            }, () =>
            {
                if (name == MethodCall.Count && node.Arguments.Count > 1)
                {
                    MakeWhereNode(node);
                }
                else
                {
                    base.Visit(node.Arguments[0]);
                }
            });

            if (buildUnion)
            {
                SQLWriter.CloseBrace();
                SQLWriter.WhiteSpace();

                SQLWriter.TableName("CTE_UNION");
            }

            return node;
        }

        private Expression SingleFieldMethod(MethodCallExpression node)
        {
            string name = node.Method.Name;

            if (name == MethodCall.Average)
            {
                name = "Avg";
            }
            else if (name == MethodCall.LongCount)
            {
                name = MethodCall.Count;
            }

            isAggregation = true;

            isNoParameterCount = name == MethodCall.Count;

            if (buildSelect)
            {
                buildSelect = false;

                if (isNoParameterCount || node.Arguments.Count > 1)
                {
                    return SingleFieldTwoArgOrCountMethod(name, node);
                }

                return SingleFieldOnlyArgMethod(name, node.Arguments[0]);
            }

            if (isNoParameterCount || node.Arguments.Count > 1)
            {
                SQLWriter.Write(name);
                SQLWriter.OpenBrace();

                if (isNoParameterCount || node.Arguments.Count == 1)
                {
                    if (buildJoin || buildUnion)
                    {
                        SQLWriter.Write("1");
                    }
                    else
                    {
                        var tableInfo = MakeTableInfo(node.Arguments[0].Type);

                        if (tableInfo.Keys.Count() == 1)
                        {
                            string prefix = GetOrAddTablePrefix(tableInfo.TableType);

                            WriteMembers(prefix, tableInfo.ReadOrWrites.Where(x => tableInfo.Keys.Contains(x.Key)));
                        }
                        else
                        {
                            SQLWriter.Write("1");
                        }
                    }
                }
                else
                {
                    base.Visit(node.Arguments[1]);
                }

                SQLWriter.CloseBrace();

                return node;
            }

            return BuildSingleOneArgField(name, node);
        }

        private Expression JoinWhereMethod(Expression node)
        {
            if (node.NodeType == ExpressionType.Call)
            {
                while (node is MethodCallExpression methodCall)
                {
                    if (methodCall.Method.Name == MethodCall.Join)
                    {
                        break;
                    }

                    if (methodCall.Method.Name == MethodCall.Where)
                    {
                        SQLWriter.WriteAnd();

                        BuildWhere = true;
                        base.Visit(methodCall.Arguments[1]);
                        BuildWhere = false;

                        node = methodCall.Arguments[0];

                        continue;
                    }

                    throw new DSyntaxErrorException($"在连接函数中，禁止使用“{methodCall.Method.Name}”函数!");
                }
            }

            return node;
        }

        private Expression JoinMethod(MethodCallExpression node)
        {
            var sb = new StringBuilder();

            var leftNode = node.Arguments[0];
            var rightNode = node.Arguments[1];

            WriteAppendAtFix(() =>
            {
                buildJoin = true;

                buildFrom = true;

                base.Visit(leftNode);

                buildFrom = true;

                base.Visit(rightNode);

                buildFrom = false;

            }, () =>
            {
                SQLWriter.Write(" ON ");

                buildFrom = false;

                base.Visit(node.Arguments[2]);

                SQLWriter.Equal();

                base.Visit(node.Arguments[3]);

                leftNode = JoinWhereMethod(node.Arguments[0]);

                rightNode = JoinWhereMethod(node.Arguments[1]);
            });

            return node;
        }

        /// <summary>
        /// 写入指定成员
        /// </summary>
        /// <param name="prefix">前缀</param>
        /// <param name="names">成员集合</param>
        protected virtual void WriteMembers(string prefix, IEnumerable<KeyValuePair<string, string>> names)
        {
            var kv = names.GetEnumerator();

            if (kv.MoveNext())
            {
                SQLWriter.Name(prefix, kv.Current.Value);

                if (!(isNoParameterCount || kv.Current.Key.ToLower() == kv.Current.Value.ToLower()))
                {
                    SQLWriter.As(kv.Current.Key);
                }

                while (kv.MoveNext())
                {
                    SQLWriter.Delimiter();

                    SQLWriter.Name(prefix, kv.Current.Value);

                    if (kv.Current.Key.ToLower() != kv.Current.Value.ToLower())
                    {
                        SQLWriter.As(kv.Current.Key);
                    }
                }
            }
        }

        private void BuildColumns(ITableInfo regions)
        {
            string prefix = GetOrAddTablePrefix(regions.TableType);

            IEnumerable<KeyValuePair<string, string>> names = regions.ReadOrWrites;

            if (buildExists && regions.Keys.Any())
            {
                names = regions.ReadOrWrites.Where(x => regions.Keys.Contains(x.Key));
            }

            if (buildCast)
            {
                names = names.Where(x => InCastList(x.Key));
            }

            if (!names.Any())
            {
                throw new DException("未指定查询字段!");
            }

            WriteMembers(prefix, names);
        }

        private void MakeFrom(ITableInfo regions)
        {
            buildFrom = false;

            _fromSwitch.Execute();

            WriteTable(regions);
        }

        private void MakeSelectFrom(Type type)
        {
            var regions = MakeTableInfo(type);

            if (buildSelect)
            {
                buildSelect = false;

                SQLWriter.Select();

                if (isDistinct)
                {
                    SQLWriter.Distinct();
                }

                BuildColumns(regions);
            }

            if (buildFrom)
            {
                MakeFrom(regions);
            }
        }

        private Expression SingleFieldOnlyArgMethod(string name, Expression node)
        {
            WriteAppendAtFix(() =>
            {
                SQLWriter.Select();

                BuildSingleOneArgField(name, node);

                if (buildUnion)
                {
                    _fromSwitch.Execute();
                    SQLWriter.OpenBrace();
                    SQLWriter.AppendAt = -1;
                    SQLWriter.CloseBrace();
                    SQLWriter.WhiteSpace();
                    SQLWriter.TableName("CTE_UNION");
                }
            }, () => base.Visit(node));

            return node;
        }

        private Expression MakeWhereNode(MethodCallExpression node)
        {
            bool whereIsNotEmpty = false;

            base.Visit(node.Arguments[0]);

            WriteAppendAtFix(() =>
            {
                if (whereIsNotEmpty)
                {
                    _whereSwitch.Execute();
                }
            }, () =>
            {
                int length = SQLWriter.Length;

                BuildWhere = true;
                base.Visit(node.Arguments[1]);
                BuildWhere = false;

                whereIsNotEmpty = SQLWriter.Length > length;
            });

            return node;
        }

        private Expression MakeExistsNode(MethodCallExpression node)
        {
            bool whereIsNotEmpty = false;

            WriteAppendAtFix(() =>
            {
                base.Visit(node.Arguments[0]);

                if (whereIsNotEmpty)
                {
                    _whereSwitch.Execute();
                }
            }, () =>
            {
                int length = SQLWriter.Length;

                BuildWhere = true;
                base.Visit(node.Arguments[1]);
                BuildWhere = false;

                whereIsNotEmpty = SQLWriter.Length > length;
            });

            return node;
        }

        private Expression MakeWhere(MethodCallExpression node)
        {
            if (buildFrom || buildSelect)
            {
                bool select = buildSelect;
                bool from = buildFrom;

                buildSelect = buildFrom = false;

                WriteAppendAtFix(() =>
                {
                    buildSelect = select;
                    buildFrom = from && !buildJoin;//Join函数

                    if (buildSelect || buildFrom)
                    {
                        MakeSelectFrom(node.Type);
                    }

                }, () => MakeWhereNode(node));

                return node;
            }

            return MakeWhereNode(node);
        }

        #region 重写
        /// <summary>
        /// 获取真实实体类型
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        protected override Type GetRealType(Type type)
        {
            if (_TypeCache.TryGetValue(type, out Type realType))
                return realType;

            return base.GetRealType(type);
        }
        /// <summary>
        /// 过滤成员
        /// </summary>
        /// <param name="members">成员集合</param>
        /// <returns></returns>
        protected override IEnumerable<MemberInfo> FilterMembers(IEnumerable<MemberInfo> members) => members.Where(x => InCastList(x.Name));
        /// <inheritdoc />
        protected override Expression VisitParameterMember(MemberExpression node)
        {
            if (inSelect)
            {
                if (!InCastList(node.Member.Name))
                    throw new DException("未指定查询字段!");

                return VisitMemberParameterSelect(node);
            }

            return base.VisitParameterMember(node);
        }
        /// <inheritdoc />
        protected virtual Expression VisitMemberParameterSelect(MemberExpression node) => base.VisitParameterMember(node);
        /// <inheritdoc />
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            var me = base.VisitMemberAssignment(node);

            SQLWriter.As(node.Member.Name);

            return me;
        }
        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type.IsQueryable())
            {
                if (buildSelect || buildFrom)
                {
                    MakeSelectFrom(node.Type);
                }
                return node;
            }

            return base.VisitConstant(node);
        }

        /// <summary>
        ///  System.Linq.Queryable 的函数
        /// </summary>
        /// <param name="node">表达式</param>
        /// <returns></returns>
        protected virtual Expression VisitQueryableMethodCall(MethodCallExpression node)
        {
            //? 函数名称
            string name = node.Method.Name;

            if (node.Arguments.Count > 1 ?
                !(name == MethodCall.Take || name == MethodCall.Skip || name == MethodCall.TakeLast || name == MethodCall.SkipLast || name == MethodCall.DefaultIfEmpty || name == MethodCall.ElementAt || name == MethodCall.ElementAtOrDefault)
                :
                (name == MethodCall.Sum || name == MethodCall.Max || name == MethodCall.Min || name == MethodCall.Average)
                )
            {
                _MethodLevel += 1;
            }

            switch (name)
            {
                case MethodCall.Any:
                    if (Parent is null)
                    {
                        SQLWriter.Select();
                    }
                    else if (Parent.BuildWhere)
                    {
                        return VisitExists(node);
                    }

                    SQLWriter.Write("CASE WHEN ");

                    VisitExists(node);

                    SQLWriter.Write(" THEN ");
                    SQLWriter.Parameter("__variable_true", true);
                    SQLWriter.Write(" ELSE ");
                    SQLWriter.Parameter("__variable_false", false);
                    SQLWriter.Write(" END");

                    return node;
                case MethodCall.All:
                    if (Parent is null)
                    {
                        SQLWriter.Select();
                    }
                    else if (Parent.BuildWhere)
                    {
                        SQLWriter.OpenBrace();

                        VisitMethodAll(node);

                        SQLWriter.CloseBrace();

                        return node;
                    }

                    SQLWriter.Write("CASE WHEN ");

                    VisitMethodAll(node);

                    SQLWriter.Write(" THEN ");
                    SQLWriter.Parameter("__variable_true", true);
                    SQLWriter.Write(" ELSE ");
                    SQLWriter.Parameter("__variable_false", false);
                    SQLWriter.Write(" END");

                    return node;
                case MethodCall.ElementAt:
                case MethodCall.ElementAtOrDefault:

                    base.Visit(node.Arguments[0]);

                    int index = (int)node.Arguments[1].GetValueFromExpression();

                    if (index < 0)
                        throw new IndexOutOfRangeException();

                    if (this.take > 0 && index < this.take)
                        throw new IndexOutOfRangeException();

                    Required = name == MethodCall.ElementAt;

                    this.take = 1;

                    this.skip += index;

                    return node;
                case MethodCall.Take:
                case MethodCall.TakeLast:

                    if (isAggregation)
                        throw new DSyntaxErrorException($"使用聚合函数时，禁止使用分页函数({name})!");

                    if (name == MethodCall.TakeLast)
                        isOrderByReverse ^= true;

                    base.Visit(node.Arguments[0]);

                    if (!isContainsOrderBy && name == MethodCall.TakeLast)
                        throw new DSyntaxErrorException($"使用函数({name})时，必须使用排序函数(OrderBy/OrderByDescending)!");

                    int take = (int)node.Arguments[1].GetValueFromExpression();

                    if (take < 1)
                        throw new ArgumentOutOfRangeException($"使用{name}函数,参数值必须大于零!");

                    if (this.take > 0 && take < this.take)
                        throw new IndexOutOfRangeException();

                    if (this.take == -1) this.take = take;

                    return node;
                case MethodCall.First:
                case MethodCall.FirstOrDefault:
                case MethodCall.Single:
                case MethodCall.SingleOrDefault:

                    // TOP(1)
                    this.take = 1;

                    if (node.Arguments.Count > 1)
                    {
                        MakeWhere(node);
                    }
                    else
                    {
                        base.Visit(node.Arguments[0]);
                    }

                    Required = name == MethodCall.First || name == MethodCall.Single;

                    return node;
                case MethodCall.Last:
                case MethodCall.LastOrDefault:

                    // TOP(..)
                    this.take = 1;

                    isOrderByReverse ^= true;

                    if (node.Arguments.Count > 1)
                    {
                        MakeWhere(node);
                    }
                    else
                    {
                        base.Visit(node.Arguments[0]);
                    }

                    if (!isContainsOrderBy)
                        throw new DSyntaxErrorException($"使用函数({name})时，必须使用排序函数(OrderBy/OrderByDescending)!");

                    Required = name == MethodCall.Last;

                    return node;
                case MethodCall.Skip:
                case MethodCall.SkipLast:

                    if (isAggregation)
                        throw new DSyntaxErrorException($"使用聚合函数时，禁止使用分页函数({name})!");

                    if (name == MethodCall.SkipLast)
                        isOrderByReverse ^= true;

                    base.Visit(node.Arguments[0]);

                    if (!isContainsOrderBy && name == MethodCall.SkipLast)
                        throw new DSyntaxErrorException($"使用函数({name})时，必须使用排序函数(OrderBy/OrderByDescending)!");

                    int skip = (int)node.Arguments[1].GetValueFromExpression();

                    if (skip < 0)
                        throw new ArgumentOutOfRangeException($"使用({name})函数,参数值不能小于零!");

                    if (this.skip == -1)
                    {
                        this.skip = skip;
                    }
                    else
                    {
                        this.skip += skip;
                    }

                    return node;
                case MethodCall.Distinct:

                    isDistinct = true;

                    return base.Visit(node.Arguments[0]);

                case MethodCall.Reverse:

                    isOrderByReverse ^= true;

                    base.Visit(node.Arguments[0]);

                    if (!isContainsOrderBy)
                        throw new DSyntaxErrorException($"使用函数({name})时，必须使用排序函数(OrderBy/OrderByDescending)!");

                    return node;
                case MethodCall.OrderBy:
                case MethodCall.ThenBy:
                case MethodCall.OrderByDescending:
                case MethodCall.ThenByDescending:

                    isContainsOrderBy = true;

                    base.Visit(node.Arguments[0]);

                    if (isAggregation)
                    {
                        return node;
                    }

                    if (buildUnion)
                    {
                        SQLWriter.AddWriter(orderby);
                    }

                    _orderBySwitch.Execute();

                    base.Visit(node.Arguments[1]);

                    if (isOrderByReverse ^ name.EndsWith("Descending"))
                    {
                        SQLWriter.WriteDesc();
                    }

                    if (buildUnion)
                    {
                        SQLWriter.RemoveWriter(orderby);
                    }
                    return node;
                case MethodCall.Where:
                case MethodCall.TakeWhile:
                    return MakeWhere(node);
                case MethodCall.SkipWhile:
                    return WrapNot(() =>
                    {
                        return MakeWhere(node);
                    });
                case MethodCall.Select:

                    if (_MethodLevel > 1)
                    {
                        throw new DSyntaxErrorException($"请将函数({name})置于查询最后一个包含入参的函数之后!");
                    }

                    if (isNoParameterCount)
                    {
                        return base.Visit(node.Arguments[0]);
                    }

                    buildSelect = false;

                    SQLWriter.Select();

                    buildFrom = false;

                    WriteAppendAtFix(() =>
                    {
                        if (isDistinct)
                        {
                            SQLWriter.Distinct();
                        }

                        buildFrom = inSelect = true;
                        base.Visit(node.Arguments[1]);
                        buildFrom = inSelect = false;

                    }, () => base.Visit(node.Arguments[0]));

                    return node;
                case MethodCall.Max:
                case MethodCall.Min:
                case MethodCall.Sum:
                case MethodCall.Average:
                case MethodCall.Count:
                case MethodCall.LongCount:
                    return SingleFieldMethod(node);
                case MethodCall.Join:
                    return JoinMethod(node);
                case MethodCall.Union:
                case MethodCall.Concat:
                case MethodCall.Except:
                case MethodCall.Intersect:

                    buildSelect = false;

                    if (buildUnion)
                    {
                        SQLWriter.RemoveWriter(orderby);
                    }

                    buildUnion = false;

                    VisitBuilder(node.Arguments[0]);

                    switch (name)
                    {
                        case MethodCall.Intersect:
                            SQLWriter.Write(" INTERSECT ");
                            break;
                        case MethodCall.Except:
                            SQLWriter.Write(" EXCEPT ");
                            break;
                        default:
                            SQLWriter.Write(MethodCall.Union == name ? " UNION " : " UNION ALL ");
                            break;
                    }

                    VisitBuilder(node.Arguments[1]);

                    buildUnion = true;

                    if (orderby is null)
                    {
                        orderby = new StringBuilder();
                    }

                    return node;

                case MethodCall.Cast:

                    Type type = node.Type.GetGenericArguments().First();

                    if (type.IsValueType || type == typeof(string) || typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        throw new TypeAccessInvalidException($"{name}函数泛型参数类型不能是值类型、字符串类型或迭代类型!");
                    }

                    var castToType = node.Arguments
                        .Select(x => x.Type)
                        .First();

                    if (node.Type == castToType)
                    {
                        return base.Visit(node.Arguments[0]);
                    }

                    useCast = true;

                    _TypeCache.GetOrAdd(type, _ => GetInitialType(castToType));

                    if (!buildSelect)
                    {
                        return base.Visit(node.Arguments[0]);//? 说明Cast函数在Select函数之前，不需要进行函数分析!
                    }

                    var entry = RuntimeTypeCache.Instance.GetCache(type);

                    if (_CastList is null)
                    {
                        _CastList = entry.PropertyStores
                            .Select(x => x.Name.ToLower())
                            .ToList();
                    }
                    else //? 取交集
                    {
                        _CastList = _CastList
                           .Intersect(entry.PropertyStores.Select(x => x.Name.ToLower()))
                           .ToList();
                    }

                    if (_CastList.Count == 0)
                    {
                        throw new DException("未指定查询字段!");
                    }

                    buildCast = true;

                    return base.Visit(node.Arguments[0]);
                case MethodCall.DefaultIfEmpty:
                    if (HasDefaultValue)
                    {
                        return base.Visit(node.Arguments[0]);
                    }

                    if (node.Arguments.Count > 1)
                    {
                        DefaultValue = node.Arguments[1].GetValueFromExpression();
                    }

                    HasDefaultValue = true;

                    return base.Visit(node.Arguments[0]);
                default:
                    return VisitFormatterMethodCall(node);
            }
        }

        /// <summary>
        /// Queryable 拓展方法
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        protected virtual Expression VisitQueryableExtentionsMethodCall(MethodCallExpression node)
        {
            string name = node.Method.Name;

            switch (name)
            {
                case MethodCall.From:
                    if (!(node.Arguments[1].GetValueFromExpression() is Func<ITableInfo, string> value))
                    {
                        throw new DException("指定表名称不能为空!");
                    }

                    if (!buildFrom)
                    {
                        base.Visit(node.Arguments[0]);
                    }

                    SetTableFactory(value);

                    if (buildFrom)
                    {
                        return base.Visit(node.Arguments[0]);
                    }

                    return node;
                case MethodCall.TimeOut:

                    TimeOut += (int)node.Arguments[1].GetValueFromExpression();

                    base.Visit(node.Arguments[0]);

                    return node;
                default:
                    return VisitFormatterMethodCall(node);
            }
        }
        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable))
            {
                return VisitQueryableMethodCall(node);
            }

            if (node.Method.DeclaringType == typeof(SelectExtentions))
            {
                return VisitQueryableExtentionsMethodCall(node);
            }

            return base.VisitMethodCall(node);
        }

        private Expression VisitLambdaFrom<T>(Expression<T> node)
        {
            var parameter = node.Parameters.First();

            var type = parameter.Type;

            var regions = MakeTableInfo(type);

            if (node.Body.NodeType == ExpressionType.Parameter)
            {
                GetOrAddTablePrefix(regions.TableType, parameter.Name);
            }
            else
            {
                GetOrAddTablePrefix(regions.TableType, node.Name ?? parameter.Name);
            }

            if (node.Body.NodeType == ExpressionType.Parameter)
            {
                BuildColumns(regions);
            }
            else
            {
                base.Visit(node.Body);
            }

            MakeFrom(regions);

            return node;
        }

        /// <summary>
        /// Lamda 分析
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="node">节点</param>
        /// <param name="addPrefixCache">添加前缀缓存</param>
        /// <returns></returns>
        protected override Expression VisitLambda<T>(Expression<T> node, Action<Type, string> addPrefixCache)
        {
            if (node.Parameters.Count > 1)
                throw new DSyntaxErrorException("不支持多个参数!");

            if (buildJoin || buildUnion)
            {
                base.Visit(node.Body);

                return node;
            }

            var parameter = node.Parameters[0];

            var type = parameter.Type;

            if (useCast)
            {
                type = GetInitialType(type);
            }

            addPrefixCache.Invoke(type, parameter.Name);

            if (buildFrom && (
                !buildSelect || //? 查询中使用了方法。
                node.Body.NodeType == ExpressionType.Parameter ||
                node.Body.NodeType == ExpressionType.New ||
                node.Body.NodeType == ExpressionType.MemberAccess ||
                node.Body.NodeType == ExpressionType.MemberInit)
             )
            {
                return VisitLambdaFrom(node);
            }

            base.Visit(node.Body);

            return node;
        }
        /// <summary>
        /// 过滤成员
        /// </summary>
        /// <param name="bindings">成员集合</param>
        /// <returns></returns>
        protected override IEnumerable<MemberBinding> FilterMemberBindings(IEnumerable<MemberBinding> bindings) => bindings.Where(item => InCastList(item.Member.Name));

        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (buildUnion) return node;

            return base.VisitParameter(node);
        }

        /// <summary>
        /// 创建构造器
        /// </summary>
        /// <param name="settings">SQL矫正配置</param>
        /// <returns></returns>
        protected override Builder CreateBuilder(ISQLCorrectSettings settings) => new QueryBuilder(settings);

        #endregion

        /// <summary>
        /// 是否必须
        /// </summary>
        public bool Required { protected set; get; }

        /// <summary>
        /// 是否有默认值
        /// </summary>
        public bool HasDefaultValue { private set; get; }

        /// <summary>
        /// 默认值
        /// </summary>
        public object DefaultValue { private set; get; }

        private string ToSQL(string value)
        {
            if (take > 0 || skip > 0)
                return buildUnion ?
                    settings.PageUnionSql(value, take, skip, orderby.ToString()) :
                    settings.PageSql(value, take, skip);

            if (buildUnion && orderby.Length > 0)
            {
                return string.Concat("SELECT * FROM (", value, ") ", settings.Name("CTE_UNION"), " ", orderby.ToString());
            }

            return value;
        }
        /// <summary>
        /// SQL
        /// </summary>
        /// <returns></returns>
        public override string ToSQL() => ToSQL(base.ToSQL());
    }
}

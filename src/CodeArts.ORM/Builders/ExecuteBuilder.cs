using CodeArts.ORM.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.ORM.Builders
{
    /// <summary>
    /// 执行构造器
    /// </summary>
    public class ExecuteBuilder<T> : Builder, IBuilder<T>
    {
        private static readonly ITableRegions typeRegions;
        static ExecuteBuilder()
        {
            typeRegions = MapperRegions.Resolve(typeof(T));
        }

        private class NestedQueryBuilder : QueryBuilder
        {
            private List<string> list;
            private readonly ISQLCorrectSettings settings;
            public NestedQueryBuilder(ISQLCorrectSettings settings) : base(settings) => this.settings = settings;

            public override void Evaluate(Expression node)
            {
                list = new List<string>();

                var index = SQLWriter.Length;

                base.Evaluate(node);

                if (list.Count == 0)
                    throw new DSyntaxErrorException("插入语句不支持匿名字段!");

                SQLWriter.AppendAt = index;

                SQLWriter.OpenBrace();

                SQLWriter.Write(string.Join(",", list.Select(x => settings.Name(x))));

                SQLWriter.CloseBrace();

                SQLWriter.WhiteSpace();

                SQLWriter.AppendAt = -1;
            }

            protected override void WriteMembers(string prefix, IEnumerable<KeyValuePair<string, string>> names)
            {
                var members = typeRegions.ReadWrites.Where(x => names.Any(y => x.Key == y.Key));

                if (!members.Any())
                    throw new DException("未指定查询字段!");

                list.AddRange(members.Select(x => x.Value));

                base.WriteMembers(prefix, members);
            }

            protected override Expression VisitMemberParameterSelect(MemberExpression node)
            {
                string name = node.Member.Name;

                if (node.Member.DeclaringType == typeRegions.TableType)
                {
                    if (!typeRegions.ReadWrites.TryGetValue(name, out string value))
                    {
                        throw new DSyntaxErrorException($"{name}字段不可写!");
                    }

                    list.Add(value);
                }

                return base.VisitMemberParameterSelect(node);
            }

            protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
            {
                if (!typeRegions.ReadWrites.TryGetValue(node.Member.Name, out string value))
                {
                    throw new DSyntaxErrorException($"{node.Member.Name}字段不可写!");
                }

                list.Add(value);

                return base.VisitMemberAssignment(node);
            }
        }

        private SmartSwitch _whereSwitch = null;
        private readonly ISQLCorrectSettings settings;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settings">修正配置</param>
        public ExecuteBuilder(ISQLCorrectSettings settings) : base(settings) => this.settings = settings;

        /// <summary>
        /// 表达式测评
        /// </summary>
        /// <param name="node">表达式</param>
        public override void Evaluate(Expression node)
        {
            _whereSwitch = new SmartSwitch(SQLWriter.Where, SQLWriter.WriteAnd);

            base.Evaluate(node);
        }

        /// <summary>
        /// 重写函数调用
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Executeable))
            {
                return VisitExecuteMethodCall(node);
            }

            return base.VisitMethodCall(node);
        }

        /// <summary>
        /// 执行行为
        /// </summary>
        public ExecuteBehavior Behavior { get; private set; }

        /// <summary>
        /// 重写变量分析
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (typeof(IExecuteable).IsAssignableFrom(node.Type))
                return node;

            return base.VisitConstant(node);
        }

        /// <summary>
        /// Where 条件
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
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
        /// <summary>
        /// 函数调用
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        private Expression VisitExecuteMethodCall(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.From:

                    if (!(node.Arguments[1].GetValueFromExpression() is Func<ITableRegions, string> value))
                        throw new DException("指定表名称不能为空!");

                    SetTableFactory(value);

                    base.Visit(node.Arguments[0]);

                    return node;
                case MethodCall.TimeOut:

                    TimeOut = (int)node.Arguments[1].GetValueFromExpression();

                    base.Visit(node.Arguments[0]);

                    return node;

                case MethodCall.Where:
                    if (Behavior == ExecuteBehavior.Insert)
                        throw new DSyntaxErrorException("插入语句不支持条件，请在查询器中使用条件过滤!");

                    return MakeWhereNode(node);
                case MethodCall.Update:
                    Behavior = ExecuteBehavior.Update;

                    base.Visit(node.Arguments[0]);

                    SQLWriter.AppendAt = 0;

                    SQLWriter.Update();

                    if (settings.Engine == DatabaseEngine.SqlServer || settings.Engine == DatabaseEngine.Access)
                    {
                        SQLWriter.Alias(GetOrAddTablePrefix(typeRegions.TableType));
                    }
                    else
                    {
                        WriteTable(typeRegions);
                    }

                    SQLWriter.Set();

                    base.Visit(node.Arguments[1]);

                    if (settings.Engine == DatabaseEngine.SqlServer || settings.Engine == DatabaseEngine.Access)
                    {
                        SQLWriter.From();

                        WriteTable(typeRegions);
                    }

                    SQLWriter.AppendAt = -1;

                    return node;
                case MethodCall.Delete:
                    Behavior = ExecuteBehavior.Delete;

                    base.Visit(node.Arguments[0]);

                    SQLWriter.AppendAt = 0;

                    SQLWriter.Delete();

                    SQLWriter.Alias(GetOrAddTablePrefix(typeof(T)));

                    SQLWriter.From();

                    WriteTable(typeRegions);

                    SQLWriter.AppendAt = -1;

                    return node;
                case MethodCall.Insert:
                    Behavior = ExecuteBehavior.Insert;

                    base.Visit(node.Arguments[0]);

                    SQLWriter.AppendAt = 0;
                    SQLWriter.Insert();

                    WriteTable(typeRegions);

                    SQLWriter.AppendAt = -1;

                    VisitBuilder(node.Arguments[1]);

                    return node;
            }

            throw new DSyntaxErrorException();
        }
        /// <summary>
        /// 成员分析
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            if (typeRegions.ReadWrites.TryGetValue(node.Member.Name, out string value))
            {
                SQLWriter.Name(value);
                SQLWriter.Write("=");

                return base.VisitMemberAssignment(node);
            }

            throw new DSyntaxErrorException($"{node.Member.Name}字段不可写!");
        }
        /// <summary>
        /// 创建构造器
        /// </summary>
        /// <param name="settings">SQL矫正配置</param>
        /// <returns></returns>
        protected override Builder CreateBuilder(ISQLCorrectSettings settings)
        {
            if (Behavior == ExecuteBehavior.Insert)
                return new NestedQueryBuilder(settings);

            return new QueryBuilder(settings);
        }
    }
}

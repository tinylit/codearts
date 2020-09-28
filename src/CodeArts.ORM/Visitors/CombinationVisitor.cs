using CodeArts.ORM.Exceptions;
using CodeArts.Runtime;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// Union/Concat/Except/Intersect。
    /// </summary>
    public class CombinationVisitor : BaseVisitor
    {
        private class CombinationSelectVisitor : SelectVisitor
        {
            public CombinationSelectVisitor(BaseVisitor visitor) : base(visitor)
            {
            }

            protected override void VisitNewMember(MemberInfo memberInfo, Expression memberExp, Type memberOfHostType)
            {
                base.VisitNewMember(memberInfo, memberExp, memberOfHostType);

                var tableInfo = base.MakeTableInfo(memberOfHostType);

                if (!tableInfo.ReadOrWrites.TryGetValue(memberInfo.Name, out string value))
                {
                    throw new DSyntaxErrorException($"“{memberInfo.Name}”不可读写!");
                }

                writer.As(value);
            }
        }

        /// <summary>
        /// inherit。
        /// </summary>
        public CombinationVisitor(BaseVisitor visitor) : base(visitor, true)
        {

        }

        /// <summary>
        /// inherit。
        /// </summary>
        protected override bool CanResolve(MethodCallExpression node) =>
                node.Method.Name == MethodCall.Union ||
                node.Method.Name == MethodCall.Concat ||
                node.Method.Name == MethodCall.Except ||
                node.Method.Name == MethodCall.Intersect;

        /// <summary>
        /// inherit。
        /// </summary>
        protected override Expression StartupCore(MethodCallExpression node)
        {
            using (var visitor = new CombinationSelectVisitor(this))
            {
                visitor.Startup(node.Arguments[0]);
            }

            switch (node.Method.Name)
            {
                case MethodCall.Intersect:
                    writer.Write(" INTERSECT ");
                    break;
                case MethodCall.Except:
                    writer.Write(" EXCEPT ");
                    break;
                case MethodCall.Union:
                    writer.Write(" UNION ");
                    break;
                case MethodCall.Concat:
                    writer.Write(" UNION ALL ");
                    break;
                default:
                    throw new DSyntaxErrorException($"函数“{node.Method.Name}”不被支持!");
            }

            using (var visitor = new CombinationSelectVisitor(this))
            {
                visitor.Startup(node.Arguments[1]);
            }

            return node;
        }
    }
}

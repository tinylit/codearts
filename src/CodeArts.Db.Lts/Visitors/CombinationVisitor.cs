using CodeArts.Db.Exceptions;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.Union{TSource}(IQueryable{TSource}, System.Collections.Generic.IEnumerable{TSource})"/>
    /// <seealso cref="Queryable.Concat{TSource}(IQueryable{TSource}, System.Collections.Generic.IEnumerable{TSource})"/>
    /// <seealso cref="Queryable.Except{TSource}(IQueryable{TSource}, System.Collections.Generic.IEnumerable{TSource})"/>
    /// <seealso cref="Queryable.Intersect{TSource}(IQueryable{TSource}, System.Collections.Generic.IEnumerable{TSource})"/>
    /// </summary>
    public class CombinationVisitor : BaseVisitor
    {
        private readonly SelectVisitor visitor;

        /// <inheritdoc />
        public CombinationVisitor(SelectVisitor visitor) : base(visitor, true)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) =>
            node.Method.DeclaringType == Types.Queryable
            && (
                node.Method.Name == MethodCall.Union ||
                node.Method.Name == MethodCall.Concat ||
                node.Method.Name == MethodCall.Except ||
                node.Method.Name == MethodCall.Intersect
            ) && node.Arguments.Count == 2;

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            using (var visitor = this.visitor.CreateInstance(this))
            {
                visitor.Startup(node.Arguments[0]);
            }

            switch (node.Method.Name)
            {
                case MethodCall.Intersect:
                case MethodCall.Except:
                case MethodCall.Union:
                    writer.WhiteSpace();
                    writer.Write(node.Method.Name.ToUpper());
                    writer.WhiteSpace();
                    break;
                case MethodCall.Concat:
                    writer.WhiteSpace();
                    writer.Write("UNION");
                    writer.WhiteSpace();
                    writer.Write("ALL");
                    writer.WhiteSpace();
                    break;
                default:
                    throw new DSyntaxErrorException($"函数“{node.Method.Name}”不被支持!");
            }

            using (var visitor = this.visitor.CreateInstance(this))
            {
                visitor.Startup(node.Arguments[1]);
            }
        }
    }
}

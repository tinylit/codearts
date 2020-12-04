using CodeArts.Db;
using System;
using System.Linq.Expressions;

namespace CodeArts.SqlServer.Visiters
{
    /// <summary>
    /// Convert扩展支持。
    /// </summary>
    public class ConvertVisitter : ICustomVisitor
    {
        /// <summary>
        /// 是否能解决。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        public bool CanResolve(MethodCallExpression node) => node.Arguments.Count == 1 && node.Method.DeclaringType == typeof(Convert);

        /// <summary>
        /// 分析。
        /// </summary>
        /// <param name="visitor">分析器。</param>
        /// <param name="writer">写入器。</param>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        public Expression Visit(ExpressionVisitor visitor, Writer writer, MethodCallExpression node)
        {
            if (node.Method.Name == "IsDBNull")
            {
                visitor.Visit(node.Arguments[0]);
                writer.IsNull();
                return node;
            }

            writer.Write("CONVERT");
            writer.OpenBrace();

            switch (node.Method.Name)
            {
                case "ToBoolean":
                    writer.Write("BIT");
                    break;
                case "ToByte":
                case "ToSByte":
                    writer.Write("TINYINT");
                    break;
                case "ToSingle":
                case "ToInt16":
                    writer.Write("SMALLINT");
                    break;
                case "ToInt32":
                    writer.Write("INT");
                    break;
                case "ToInt64":
                    writer.Write("BIGINT");
                    break;
                case "ToDouble":
                case "ToDecimal":
                    writer.Write("DECIMAL");
                    break;
                case "ToChar":
                    writer.Write("CHAR(1)");
                    break;
                case "ToString":
                    writer.Write("CHAR");
                    break;
                case "ToDateTime":
                    writer.Write("DATETIME");
                    break;
                case "ToUInt16":
                case "ToUInt32":
                case "ToUInt64":
                default:
                    throw new NotSupportedException();
            }
            writer.Delimiter();
            visitor.Visit(node.Arguments[0]);
            writer.CloseBrace();
            return node;
        }
    }
}

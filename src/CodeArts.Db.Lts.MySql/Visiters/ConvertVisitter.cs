using System;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts.MySql
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
        public void Visit(ExpressionVisitor visitor, Writer writer, MethodCallExpression node)
        {
            if (node.Method.Name == "IsDBNull")
            {
                visitor.Visit(node.Arguments[0]);

                writer.IsNull();

                return;
            }

            writer.Write("CONVERT");
            writer.OpenBrace();
            visitor.Visit(node.Arguments[0]);
            writer.Delimiter();

            switch (node.Method.Name)
            {
                case "ToBoolean":
                case "ToByte":
                case "ToSByte":
                case "ToSingle":
                case "ToInt16":
                case "ToInt32":
                case "ToInt64":
                    writer.Write("SIGNED");
                    break;
                case "ToDouble":
                case "ToDecimal":
                    writer.Write("DECIMAL");
                    break;
                case "ToUInt16":
                case "ToUInt32":
                case "ToUInt64":
                    writer.Write("UNSIGNED");
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
                default:
                    throw new NotSupportedException();
            }
            writer.CloseBrace();
        }
    }
}

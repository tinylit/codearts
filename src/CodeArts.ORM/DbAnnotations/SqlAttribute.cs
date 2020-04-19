using CodeArts.ORM;
using System;
using System.Linq;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 语句
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SqlAttribute : CommandAttribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="commandType">命令类型</param>
        public SqlAttribute(ISQL sql, CommandKind commandType) : base(commandType)
        {
            UppercaseString uppercaseString;

            switch (commandType)
            {
                case CommandKind.Insert:
                    uppercaseString = CommandTypes.Insert;
                    break;
                case CommandKind.Delete:
                    uppercaseString = CommandTypes.Delete;
                    break;
                case CommandKind.Query:
                    uppercaseString = CommandTypes.Select;
                    break;
                case CommandKind.Update:
                    uppercaseString = CommandTypes.Update;
                    break;
                default:
                    uppercaseString = commandType.ToString();
                    break;
            }

            if (Sql.Tables.All(x => x.CommandType == uppercaseString))
            {
                Sql = sql;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// SQL
        /// </summary>
        public ISQL Sql { get; }
    }
}

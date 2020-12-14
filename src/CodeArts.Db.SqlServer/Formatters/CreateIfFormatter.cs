using System;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeArts.Db.Formatters
{
    /// <summary>
    /// 条件创建。
    /// </summary>
    public class CreateIfFormatter : AdapterFormatter<CreateIfFormatter>, IFormatter
    {
        private static readonly Regex PatternCreateIf = new Regex(@"\bcreate[\x20\t\r\n\f]+(?<command>table|view|function|procedure|database)[\x20\t\r\n\f]+(?<if>if[\x20\t\r\n\f]+not[\x20\t\r\n\f]+exists[\x20\t\r\n\f]+)([\w\[\]]+\.)*\[(?<name>\w+)\][\x20\t\r\n\f]*\(((?!\b(select|insert|update|delete|create|drop|alter|truncate|use)\b)(on[\x20\t\r\n\f]+(update|delete)|[^;]))+;?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 构造函数。
        /// </summary>
        public CreateIfFormatter() : base(PatternCreateIf)
        {
        }

        /// <summary>
        /// 条件创建。
        /// </summary>
        /// <param name="item">匹配项。</param>
        /// <param name="command">命令。</param>
        /// <param name="if">条件。</param>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public string CreateIf(Match item, string command, Group @if, string name)
        {
            var value = item.Value;

            var sb = new StringBuilder();

            switch (command.ToUpper())
            {
                case "VIEW":
                    return sb.AppendFormat("IF EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='V' AND [name] ='{0}')", name)
                            .AppendLine()
                            .Append("   DROP VIEW ")
                            .Append("[")
                            .Append(name)
                            .Append("]")
                            .AppendLine()
                            .Append("GO")
                            .AppendLine()
                            .Append(value.Substring(0, @if.Index - item.Index))
                            .Append(value.Substring(@if.Index - item.Index + @if.Length))
                            .AppendLine()
                            .Append("GO")
                            .ToString();
                case "TABLE":
                    sb.AppendFormat("IF NOT EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='U' AND [name] ='{0}')", name);
                    break;
                case "FUNCTION":
                    sb.AppendFormat("IF NOT EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype] IN ('FN', 'IF', 'TF') AND [name] ='{0}')", name);
                    break;
                case "PROCEDURE":
                    sb.AppendFormat("IF NOT EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='P' AND [name] ='{0}')", name);
                    break;
                case "DATABASE":
                    sb.AppendFormat("IF NOT EXIXSTS(SELECT * FROM [sys].[databases] WHERE [name]='{0}')", name);
                    break;
                default:
                    throw new NotSupportedException();
            }

           return sb.Append(" BEGIN")
            .AppendLine()
            .Append(value.Substring(0, @if.Index - item.Index))
            .Append(value.Substring(@if.Index - item.Index + @if.Length))
            .AppendLine()
            .Append("END GO")
            .AppendLine()
            .ToString();
        }
    }
}

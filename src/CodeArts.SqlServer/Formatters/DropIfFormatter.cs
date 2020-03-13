using CodeArts.ORM;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeArts.SqlServer.Formatters
{
    /// <summary>
    /// 
    /// </summary>
    public class DropIfFormatter : AdapterFormatter<DropIfFormatter>, IFormatter
    {
        private static readonly Regex PatternDropIf = new Regex(@"\bdrop[\x20\t\r\n\f]+(?<command>table|view|function|procedure|database)[\x20\t\r\n\f]+(?<if>if[\x20\t\r\n\f]+exists[\x20\t\r\n\f]+)([\[\w\]]+\.)*\[(?<name>\w+)\][\x20\t\r\n\f]*;?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// 构造函数
        /// </summary>
        public DropIfFormatter() : base(PatternDropIf)
        {
        }

        /// <summary>
        /// 条件删除。
        /// </summary>
        /// <param name="item">匹配内容</param>
        /// <param name="command">命令</param>
        /// <param name="if">条件</param>
        /// <param name="name">表或视图名称</param>
        /// <returns></returns>
        public string DropIf(Match item, string command, Group @if, string name)
        {
            var value = item.Value;

            var sb = new StringBuilder();

            switch (command.ToUpper())
            {
                case "TABLE":
                    sb.AppendFormat("IF EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='U' and [name] ='{0}')", name);
                    break;
                case "VIEW":
                    sb.AppendFormat("IF EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='V' and [name] ='{0}')", name);
                    break;
                case "FUNCTION":
                    sb.AppendFormat("IF EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype] IN('FN', 'IF', 'TF') AND [name] ='{0}')", name);
                    break;
                case "PROCEDURE":
                    sb.AppendFormat("IF EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='P' AND [name] ='{0}')", name);
                    break;
                case "DATABASE":
                    sb.AppendFormat("IF EXIXSTS(SELECT * FROM [sys].[databases] WHERE [name]='{0}')", name);
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

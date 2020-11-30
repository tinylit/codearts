using System.Text.RegularExpressions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 格式化。
    /// </summary>
    public interface IFormatter
    {
        /// <summary>
        /// 表达式。
        /// </summary>
        Regex RegularExpression { get; }

        /// <summary>
        /// 替换内容。
        /// </summary>
        /// <param name="match">匹配到的内容。</param>
        /// <returns></returns>
        string Evaluator(Match match);
    }
}

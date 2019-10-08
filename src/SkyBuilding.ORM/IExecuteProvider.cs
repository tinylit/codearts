using System.Linq.Expressions;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 执行器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IExecuteProvider<T>
    {
        /// <summary>
        /// 创建更新
        /// </summary>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        IExecuteable<T> CreateExecute(Expression expression);

        /// <summary>
        /// 执行影响行数
        /// </summary>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        int Execute(Expression expression);
    }
}
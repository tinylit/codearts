using System;
using System.Transactions;

namespace CodeArts.Db.DependencyInjection
{
    /// <summary>
    /// 标记使用事务。
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
    public class TransactionAttribte : Attribute
    {
        /// <summary>
        /// 默认：<see cref="TransactionScopeOption.Required"/>。
        /// </summary>
        public TransactionAttribte() : this(TransactionScopeOption.Required)
        {
        }

        /// <summary>
        /// 指定事务范围配置。
        /// </summary>
        /// <param name="transactionScope">事务范围配置。</param>
        public TransactionAttribte(TransactionScopeOption transactionScope)
        {
            TransactionScope = transactionScope;
        }

        /// <summary>
        /// 事务范围配置。
        /// </summary>
        public TransactionScopeOption TransactionScope { get; }
    }
}

#if NET_CORE
using Microsoft.EntityFrameworkCore.Storage;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// 指令构建器工厂。
    /// </summary>
    public class DbRelationalCommandBuilderFactory : RelationalCommandBuilderFactory
    {
        /// <summary>
        /// inheritdoc.
        /// </summary>
        public DbRelationalCommandBuilderFactory(RelationalCommandBuilderDependencies dependencies) : base(dependencies)
        {
        }

        /// <summary>
        /// inheritdoc.
        /// </summary>
        public override IRelationalCommandBuilder Create() => new DbRelationalCommandBuilder(Dependencies);
    }
}
#endif

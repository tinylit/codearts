#if NET_CORE
using Microsoft.EntityFrameworkCore.Storage;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// 指令构建器。
    /// </summary>
    public class DbRelationalCommandBuilder : RelationalCommandBuilder
    {
        /// <summary>
        /// inheritdoc.
        /// </summary>
        public DbRelationalCommandBuilder(RelationalCommandBuilderDependencies dependencies) : base(dependencies)
        {
        }

        /// <summary>
        /// inheritdoc.
        /// </summary>
        public override IRelationalCommand Build() => new DbRelationalCommand(Dependencies, ToString(), Parameters);
    }
}
#endif
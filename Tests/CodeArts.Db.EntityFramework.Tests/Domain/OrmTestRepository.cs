using CodeArts.Db.EntityFramework;
using CodeArts.Db.Tests.Domain.Entities;
using UnitTest.Serialize;

namespace CodeArts.Db.Tests.Domain
{
    [DefaultDbConfig]
    public class OrmTestRepository : Repository<OrmTest>
    {
    }
}

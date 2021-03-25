using CodeArts.Db.Lts;
using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace CodeArts.Db.Domain
{
    [SqlServerConnection]
    public class UserRepository : DbRepository<FeiUsers>
    {
    }
}

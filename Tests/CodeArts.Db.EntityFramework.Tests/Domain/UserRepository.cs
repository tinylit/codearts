using CodeArts.Db.EntityFramework;
using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace CodeArts.Db.Domain
{
    [SqlServerConnection]
    public class UserRepository : Repository<FeiUsers>
    {
    }
}

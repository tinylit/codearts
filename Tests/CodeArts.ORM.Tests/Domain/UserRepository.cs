using CodeArts.ORM.Tests;
using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace CodeArts.ORM.Domain
{
    [SqlServerConnection]
    public class UserRepository : DbRepository<FeiUsers>
    {
    }
}

using CodeArts.Db.Lts;
using System.Collections.Generic;
using System.Linq;
using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace CodeArts.Db.Domain
{
    [SqlServerConnection]
    public class UserRepository : DbRepository<FeiUsers>
    {
    }
}

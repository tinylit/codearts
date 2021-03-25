using CodeArts.Db.Lts;
using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace UnitTest.Domain
{
    [DefaultDbConfig]
    public class AuthShipRepository : DbRepository<AuthShip>
    {
    }
}

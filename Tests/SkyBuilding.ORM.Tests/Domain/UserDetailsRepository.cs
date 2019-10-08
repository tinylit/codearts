using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace SkyBuilding.ORM.Domain
{
    [SqlServerConnection]
    public class UserDetailsRepository : Repository<FeiUserdetails>
    {
    }
}

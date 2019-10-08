using SkyBuilding.ORM;
using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace UnitTest.Domain
{
    [DefaultDbConfig]
    public class OrgTreeRepository : DbRepository<OrgTree>
    {
    }
}

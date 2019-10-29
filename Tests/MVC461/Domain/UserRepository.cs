using Mvc461.Domain.Entities;
using SkyBuilding.ORM;

namespace Mvc461.Domain
{
    [DbConfig("default")]
    public class UserRepository : DbRepository<User>
    {

    }
}

using Mvc.Core.Domain.Entities;
using SkyBuilding.ORM;

namespace Mvc.Core.Domain
{
    [DbConfig("connectionStrings:default")]
    public class UserRepository : DbRepository<User>
    {

    }
}

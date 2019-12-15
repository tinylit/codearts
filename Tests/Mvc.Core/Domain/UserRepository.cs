using Mvc.Core.Domain.Entities;
using CodeArts.ORM;

namespace Mvc.Core.Domain
{
    [DbConfig("connectionStrings:default")]
    public class UserRepository : DbRepository<User>
    {

    }
}

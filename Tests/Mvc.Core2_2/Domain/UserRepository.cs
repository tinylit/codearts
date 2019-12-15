using Mvc.Core2_2.Domain.Entities;
using CodeArts.ORM;

namespace Mvc.Core2_2.Domain
{
    [DbConfig("connectionStrings:default")]
    public class UserRepository : DbRepository<User>
    {

    }
}

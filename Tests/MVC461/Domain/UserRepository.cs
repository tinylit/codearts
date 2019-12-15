using Mvc461.Domain.Entities;
using CodeArts.ORM;

namespace Mvc461.Domain
{
    [DbConfig("connectionStrings:default")]
    public class UserRepository : DbRepository<User>
    {
    }
}

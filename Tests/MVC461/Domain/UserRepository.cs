using Mvc461.Domain.Entities;
using CodeArts.ORM;

namespace Mvc461.Domain
{
    /// <summary>
    /// 用户仓库
    /// </summary>
    [DbConfig("connectionStrings:default")]
    public class UserRepository : DbRepository<User>
    {
    }
}

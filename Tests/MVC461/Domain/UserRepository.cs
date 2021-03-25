using CodeArts.Db;
using CodeArts.Db.Lts;
using Mvc461.Domain.Entities;

namespace Mvc461.Domain
{
    /// <summary>
    /// 用户仓库。
    /// </summary>
    [DbConfig]
    public class UserRepository : DbRepository<User>
    {
    }
}

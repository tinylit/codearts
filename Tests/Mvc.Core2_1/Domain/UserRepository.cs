using Mvc.Core2_1.Domain.Entities;
using CodeArts.Db;
using CodeArts.Db.Lts;

namespace Mvc.Core2_1.Domain
{
    /// <summary>
    /// 用户仓库。
    /// </summary>
    [DbConfig]
    public class UserRepository : DbRepository<User>
    {

    }
}

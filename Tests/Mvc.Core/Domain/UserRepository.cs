using Mvc.Core.Domain.Entities;
using CodeArts.Db;
using CodeArts.Db.Lts;

namespace Mvc.Core.Domain
{
    /// <summary>
    /// 用户仓库。
    /// </summary>
    [DbConfig("connectionStrings:default")]
    public class UserRepository : DbRepository<User>
    {

    }
}

using Mvc.Core2_1.Domain.Entities;
using CodeArts.Db;

namespace Mvc.Core2_1.Domain
{
    /// <summary>
    /// 用户仓库。
    /// </summary>
    [DbConfig("connectionStrings:default")]
    public class UserRepository : DbRepository<User>
    {

    }
}

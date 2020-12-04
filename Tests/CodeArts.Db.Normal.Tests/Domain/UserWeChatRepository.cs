using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace CodeArts.Db.Domain
{
    [SqlServerConnection]
    public class UserWeChatRepository : DbRepository<FeiUserWeChat>
    {
    }
}

using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace CodeArts.ORM.Domain
{
    [SqlServerConnection]
    public class UserWeChatRepository : DbRepository<FeiUserWeChat>
    {
    }
}

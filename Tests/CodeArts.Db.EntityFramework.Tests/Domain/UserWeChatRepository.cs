using CodeArts.Db.EntityFramework;
using UnitTest.Domain.Entities;
using UnitTest.Serialize;
#if NET461
using System.Data.Entity;
#else
using Microsoft.EntityFrameworkCore;
#endif

namespace CodeArts.Db.Domain
{
    [SqlServerConnection]
    public class UserWeChatRepository : LinqRepository<FeiUserWeChat>
    {
        public UserWeChatRepository(DbContext context) : base(context)
        {
        }
    }
}

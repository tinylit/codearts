using CodeArts.Db.EntityFramework;
#if NET461
using System.Data.Entity;
#else
using Microsoft.EntityFrameworkCore;
#endif
using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace CodeArts.Db.Domain
{
    [SqlServerConnection]
    public class UserRepository : LinqRepository<FeiUsers>
    {
        public UserRepository(DbContext context) : base(context)
        {
        }
    }
}

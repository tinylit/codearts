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
    public class UserDetailsRepository : LinqRepository<FeiUserdetails>
    {
        public UserDetailsRepository(DbContext context) : base(context)
        {
        }
    }
}

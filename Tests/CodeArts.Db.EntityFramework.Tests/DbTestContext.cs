#if NET461
using System.Data.Entity;
#else
using Microsoft.EntityFrameworkCore;
#endif
using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace CodeArts.Db.EntityFramework.Tests
{
    [SqlServerConnection]
    public class DbTestContext : DbContext<DbTestContext>
    {
        public DbSet<FeiUserdetails> FeiUserdetails { get; set; }

        public DbSet<FeiUsers> FeiUsers { get; set; }

        public DbSet<FeiUserWeChat> FeiUserWeChat { get; set; }
    }
}

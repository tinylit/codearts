using CodeArts.Db.Dapper.Tests.Domain.Entities;
using UnitTest.Serialize;

namespace CodeArts.Db.Dapper.Tests
{
    [SqlServerConnection]
    public class SkyDbContext : DbContext
    {
        public DbServiceSet<FeiUsers> Users { get; set; }

        public DbServiceSet<FeiUserdetails> Userdetails { get; set; }
    }
}
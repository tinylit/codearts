using CodeArts.Db.Tests.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using UnitTest.Serialize;

namespace CodeArts.Db.Tests.Domain
{
    [DefaultDbConfig]
    public class OrmTestRepository : DbRepository<OrmTest>
    {
    }
}

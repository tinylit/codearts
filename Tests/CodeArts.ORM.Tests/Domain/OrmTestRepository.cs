using CodeArts.ORM.Tests.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using UnitTest.Serialize;

namespace CodeArts.ORM.Tests.Domain
{
    [DefaultDbConfig]
    public class OrmTestRepository : DbRepository<OrmTest>
    {
    }
}

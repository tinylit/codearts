using CodeArts.Db.Dapper.Tests.Domain.Entities;
using NUnit.Framework;
using System;

namespace CodeArts.Db.Dapper.Tests
{
    public class Tests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            DapperConnectionManager.RegisterAdapter(new SqlServerAdapter());
        }

        [Test]
        public void SimpleTest()
        {
            string sql = @"SELECT TOP 10 * FROM fei_users";

            using (var context = new SkyDbContext())
            {
                var results = context.Query<FeiUsers>(sql);
            }
        }

        [Test]
        public void JoinPagingTest()
        {
            string sql = @"SELECT
	                            x.* 
                            FROM
	                            fei_users x
	                            INNER JOIN fei_userdetails y ON x.uid = y.uid
                            WHERE x.userstatus = 1";

            using (var context = new SkyDbContext())
            {
                var results = context.Query<FeiUsers>(sql, 0, 10);
            }
        }

        [Test]
        public void ComprehensiveTest()
        {
            int bcid = (new Random()).Next(100, 1000);
            string name = DateTime.Now.Ticks.ToString();

            string sql = @"SELECT uid AS id
                            ,bcid
                            ,username
                            ,email
                            ,mobile
                            ,password
                            ,mallagid
                            ,salt
                            ,userstatus
                            ,created_time
                            ,modified_time
                            ,actionlist
                            FROM fei_users
                            WHERE bcid = @bcid";

            using (var context = new SkyDbContext())
            {
                int insertCount = context.Users.AsInsertable(new FeiUsers
                {
                    Bcid = bcid,
                    Username = name,
                    Userstatus = 1,
                    Mobile = "18980861011",
                    Email = "tinylit@foxmail.com",
                    Password = "123456",
                    Salt = string.Empty,
                    CreatedTime = DateTime.Now,
                    ModifiedTime = DateTime.Now
                }).ExecuteCommand();

                var entry = context.QuerySingle<FeiUsers>(sql, new { bcid, name });

                entry.Userstatus = 2;
                entry.ModifiedTime = DateTime.Now;

                int updateCount = context.Users.AsUpdateable(entry)
                    .Set(x => new { x.Userstatus, x.ModifiedTime })
                    .ExecuteCommand();

                int deleteCount = context.Users.AsDeleteable(entry)
                    .ExecuteCommand();

                Assert.AreEqual(insertCount, updateCount);
                Assert.AreEqual(insertCount, deleteCount);
            }
        }
    }
}
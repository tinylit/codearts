using CodeArts;
using CodeArts.Casting;
using CodeArts.Db;
using CodeArts.Db.Domain;
using CodeArts.Db.EntityFramework;
using CodeArts.Db.EntityFramework.Tests;
using CodeArts.Db.Tests;
#if NET461
using System.Data.Entity;
#else
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTest.Domain.Entities;

namespace UnitTest
{
    [TestClass]
    public class SqlServerTest
    {

        private static readonly DbContext context;

        static SqlServerTest() => context = new DbTestContext();

        private static bool isCompleted;

        [TestInitialize]
        public void Initialize()
        {
#if NET461
            var adapter = new CodeArts.Db.SqlServerAdapter();
#else
            var adapter = new CodeArts.Db.EntityFramework.SqlServerLinqAdapter();
#endif
            LinqConnectionManager.RegisterAdapter(adapter);

            RuntimeServPools.TryAddSingleton<IMapper, CastingMapper>();

            if (isCompleted) return;

            var connectionString = string.Format(@"Server={0};Database={1};User ID={2};Password={3}",
                SqlServerConsts.Domain,
                SqlServerConsts.Database,
                SqlServerConsts.User,
                SqlServerConsts.Password);

            using (var connection = TransactionConnections.GetConnection(connectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionString, adapter))
            {
                try
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = System.Data.CommandType.Text;

                        var files = Directory.GetFiles("../../../Sql", "*.sql");

                        foreach (var file in files.Where(x => x.Contains("mssql")))
                        {
                            string text = File.ReadAllText(file, Encoding.UTF8);

                            command.CommandText = text;

                            command.ExecuteNonQuery();
                        }
                    }

                    isCompleted = true;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        [TestMethod]
        public void SelectTest()
        {
            var y = 100;
            var user = new UserRepository(context);

            var result = user.Select(x => new { x.Id, OldId = x.Id + 1, OOID = y });//.Where(x => x.Id > 0 && x.Id < y);

            var list = result.ToList();

        }
        [TestMethod]
        public void WhereTest()
        {
            var y = 100;
            var user = new UserRepository(context);
            var result = user.Where(x => x.Id > 0 && x.Id < y);

            var list = result.ToList();
        }

        [TestMethod]
        public void SelectWhereTest()
        {
            var y = 100;
            var user = new UserRepository(context);
            var result = user.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Username });

            var list = result.ToList();
        }

        [TestMethod]
        public void SingleOrDefaultWithArg()
        {
            var y = 100;
            var user = new UserRepository(context);
            var result = user.SingleOrDefault(x => x.Id < y && x.Userstatus > 0);
        }

        [TestMethod]
        public void UnionTest()
        {
            var y = 100;
            var user = new UserRepository(context);
            var details = new UserDetailsRepository(context);
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, Name = x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Realname });

            var list = result.Union(result2).ToList();
        }

        [TestMethod]
        public void UnionCountTest()
        {
            var y = 100;
            var user = new UserRepository(context);
            var details = new UserDetailsRepository(context);
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, Name = x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Realname });

            var count = result.Union(result2).Count();
        }

        [TestMethod]
        public void AnyTest()
        {
            var user = new UserRepository(context);
            var has = user.Any(x => x.Id < 100);
        }
        [TestMethod]
        public void AllTest()
        {
            var user = new UserRepository(context);
            var has = user.Where(x => x.Id < 100).All(x => x.Id < 100);
        }

        [TestMethod]
        public void AvgTest()
        {
            var user = new UserRepository(context);
            var has = user.Where(x => x.Id < 100).Average(x => x.Id);
        }

        [TestMethod]
        public void LikeTest()
        {
            var y = 100;
            var user = new UserRepository(context);
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("liu")).Select(x => new { x.Id, OldId = x.Id + 1, OOID = y });

            var list = result.ToList();
        }
        [TestMethod]
        public void INTest()
        {
            var y = 100;
            var user = new UserRepository(context);
            var arr = new List<string> { "1", "2" };
            var result = user.Where(x => x.Id > 0 && x.Id < y && arr.Contains(x.Username))
                .Select(x => new { x.Id, OldId = x.Id + 1, OOID = y });

            var list = result.ToList();
        }

        [TestMethod]
        public void INSQLTest()
        {
            var y = 100;
            var user = new UserRepository(context);
            var details = new UserDetailsRepository(context);
            var result = user.Where(x => x.Id > 0 && x.Id < y && details.Select(z => z.Nickname).Contains(x.Username))
                .Select(x => new { x.Id, OldId = x.Id + 1, OOID = y });

            var list = result.ToList();
        }

        [TestMethod]
        public void INTest2()
        {
            var y = 100;
            var user = new UserRepository(context);
            var details = new UserDetailsRepository(context);
            var arr = new string[] { "1", "2" };
            var result = user.Where(x => x.Id > 0 && x.Id < y && arr.Any() && details.Any(z => z.Nickname == x.Username))
                .Select(x => new { x.Id, OldId = x.Id + 1, OOID = y });

            var list = result.ToList();
        }
        [TestMethod]
        public void AnyINTest()
        {
            var y = 100;
            var user = new UserRepository(context);
            var arr = new List<int> { 1, 10 };
            var result = user.Where(x => x.Id > 0 && x.Id < y && arr.Any(item => item == x.Id))
                .Select(x => new { x.Id, OldId = x.Id + 1, OOID = y });

            var list = result.ToList();
        }

        [TestMethod]
        public void IIFTest()
        {
            var y = 100;
            string cc = null;
            var user = new UserRepository(context);

            var result = user.Where(x => x.Id > 0 && x.Id < y)
                .Select(x => new { Id = x.Id > 5000 ? x.Id : x.Id + 5000, OldId = x.Id + 1, OOID = y > 0 ? y : 100, DD = cc ?? string.Empty });

            var value = result.ToString();
            var list = result.ToList();
        }

        [TestMethod]
        public void OrderByTest()
        {
            var user = new UserRepository(context);
            var result = user.OrderBy(x => x.CreatedTime);
            var list = result.ToList();
        }
        [TestMethod]
        public void WhereOrderByTest()
        {
            var str = "1";
            var user = new UserRepository(context);
            var result = user.Where(x => x.Id < 200 && x.Username.Contains(str) && x.CreatedTime < DateTime.Now)
                .OrderBy(x => x.CreatedTime);
            var list = result.ToList();
        }

        [TestMethod]
        public void WhereHasValueTest()
        {
            var str = "1";
            int? i = 1;
            var user = new UserRepository(context);
            var result = user.Where(x => x.Id < 200 && x.Username.Contains(str) && (i.HasValue && x.Id > i.Value) && x.CreatedTime < DateTime.Now && x.Userstatus.HasValue);

            result = result.Where(x => x.Mallagid == 2);

            var list = result.OrderBy(x => x.CreatedTime).ToList();
        }
        [TestMethod]
        public void OrderBySelect()
        {
            var str = "1";
            var user = new UserRepository(context);
            var result = user
#if NET461
                .Where(x => x.Id < 200 && x.Username.Contains(str) && x.CreatedTime < DateTime.Now);
#else
                .Where(x => x.Id < 200 && EF.Functions.Like(x.Username, $"%{str}%") && x.CreatedTime < DateTime.Now);
#endif
            var list = result.OrderBy(x => x.CreatedTime).ToList();
            var count = result.Count();
        }
        [TestMethod]
        public void ExistsTest()
        {
            var user = new UserRepository(context);
            var details = new UserDetailsRepository(context);
            var result = user.Where(x => x.Id < 200 && details.Distinct().Any(y => y.Id == x.Id && y.Id < 100))
                .OrderBy(x => x.CreatedTime)
                .Distinct()
                .OrderByDescending(x => x.Bcid);
            var list = result.ToList();
        }

        [TestMethod]
        public void MaxWithTest()
        {
            var user = new UserRepository(context);
            var result = user.Max(x => x.Id);
        }

        [TestMethod]
        public void DistinctTest()
        {
            var user = new UserRepository(context);
            var result = user.Distinct();
            var list = result.ToList();
        }

        [TestMethod]
        public void DistinctSelectTest()
        {
            var user = new UserRepository(context);
            var result = user.Distinct().Select(x => x.Id);
            var list = result.ToList();
        }

        [TestMethod]
        public void DistinctWhereTest()
        {
            var user = new UserRepository(context);
            var result = user.Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now).Distinct();
            var list = result.ToList();
        }

        [TestMethod]
        public void JoinTest()
        {
            var y = 100;
            var str = "1";
            var user = new UserRepository(context);
            var details = new UserDetailsRepository(context);
            var result = from x in user
                         join d in details on x.Id equals d.Id
                         where x.Id > 0 && d.Id < y && x.Username.Contains(str)
                         orderby x.Id, d.Registertime descending
                         select new { x.Id, OldId = x.Id + 1, OOID = d.Id, DDD = y };

            var list = result.ToList();
        }

        [TestMethod]
        public void JoinTest2()
        {
            var y1 = 100;
            var str = "1";
            var user = new UserRepository(context);
            var details = new UserDetailsRepository(context);
            var userWx = new UserWeChatRepository(context);
            var result = from x in user
                         join y in details on x.Id equals y.Id
                         join z in userWx on x.Id equals z.Uid
                         where x.Id > 0 && y.Id < y1 && x.Username.Contains(str)
                         orderby x.Id, y.Registertime descending
                         select new { x.Id, OldId = x.Id + 1, z.Openid };

            var list = result.ToList();
        }

        [TestMethod]
        public void JoinTest3()
        {
            var y = 100;
            var str = "1";
            FeiUserWeChatStatusEnum? status = null;
            var user = new UserRepository(context);
            var details = new UserDetailsRepository(context);
            var userWx = new UserWeChatRepository(context);
            var result = from x in user
                         join d in details
                         on x.Id equals d.Id
                         join w in userWx.Where(w => w.Status == status) on x.Id equals w.Uid
                         where x.Id > 0 && d.Id < y && x.Username.Contains(str)
                         orderby x.Id, d.Registertime descending
                         select new { x.Id, OldId = x.Id + 1, w.Openid, OOID = d.Id, DDD = y };

            var list = result.ToList();
        }

        [TestMethod]
        public void JoinTest4()
        {
            var y = 100;
            var str = "1";
            var user = new UserRepository(context);
            var details = new UserDetailsRepository(context);
            var userWx = new UserWeChatRepository(context);
            var result = from x in user
                         join d in details.Where(d => d.Id < y)
                         on x.Id equals d.Id
                         join w in userWx
                         on x.Id equals w.Uid
                         where x.Id > 0 && x.Username.Contains(str)
                         orderby x.Id, d.Registertime descending
                         select new { x.Id, OldId = x.Id + 1, w.Openid, OOID = d.Id, DDD = y };

            var list = result.Count();
        }

        [TestMethod]
        public void JoinCountTest2()
        {
            var id = 100;
            var str = "1";
            var user = new UserRepository(context);
            var details = new UserDetailsRepository(context);
            var result = from x in user
                         join y in details on x.Id equals y.Id
                         where x.Id > 0 && y.Id < id && x.Username.Contains(str)
                         select new { x.Id, y.Nickname };
            var list = result.Count();
        }


        [TestMethod]
        public void TakeTest()
        {
            var user = new UserRepository(context);
            var result = user.Take(10);
            var list = result.ToList();
        }
        [TestMethod]
        public void TakeWhereTest()
        {
            var user = new UserRepository(context);
            var result = user.Take(10).Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now);
            var list = result.ToList();
        }

        [TestMethod]
        public void FirstOrDefaultTest()
        {
            var user = new UserRepository(context);
            var userEntity = user.FirstOrDefault();
        }
        [TestMethod]
        public void FirstOrDefaultWhereTest()
        {
            var user = new UserRepository(context);

            var userEntity = user.Where(x => x.Username.Length > 10 && x.Id < 100 && x.CreatedTime < DateTime.Now)
                .OrderBy(x => x.Id)
                .FirstOrDefault();
        }

        [TestMethod]
        public void IntersectTest()
        {
            var y = 100;
            var user = new UserRepository(context);
            var details = new UserDetailsRepository(context);
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Username = x.Realname });

            var list = result.Intersect(result2).Take(10).ToList();

        }
        [TestMethod]
        public void IntersectCountTest()
        {
            var y = 100;
            var user = new UserRepository(context);
            var details = new UserDetailsRepository(context);
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Username = x.Realname });

            var Count = result.Intersect(result2).Count();
        }

        [TestMethod]
        public void NullableWhereTest() //? 当条件的一端为Null，且另一端为值类型时，条件会被自动忽略。
        {
            var y = 100;

            DateTime? date = null;

            var user = new UserRepository(context);
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.CreatedTime > date)
                .Select(x => new { x.Id, Name = x.Username });

            var list = result.ToList();
        }

        [TestMethod]
        public void BitOperationTest()
        {
            var user = new UserRepository(context);

            var result = user.Where(x => (x.Userstatus & 1) == 1 && x.Id < 100).Take(10);
            var list = result.ToList();
        }

        [TestMethod]
        public void CountWithArgumentsTest()
        {
            var user = new UserRepository(context);

            var count = user.Count(x => x.Mallagid == 2);
        }

        [TestMethod]
        public void JoinInJoinTest()
        {
            var user = new UserRepository(context);
            var userdetails = new UserDetailsRepository(context);

            var joinRight = from x in user
                            join y in userdetails
                            on x.Id equals y.Id
                            where x.Id < 10000
                            select x;

            var linq = from x in userdetails
                       join y in joinRight
                       on x.Id equals y.Id
                       where y.Mallagid > 0
                       orderby y.Mallagid
                       orderby x.Id
                       select new
                       {
                           x.Id,
                           y.Mallagid
                       };


            var results = linq.ToList();
        }

        [TestMethod]
        public void UnoinInJoinTest()
        {
            var user = new UserRepository(context);
            var userdetails = new UserDetailsRepository(context);

            var unionRight = (from x in user
                              join y in userdetails
                              on x.Id equals y.Id
                              where x.Id < 10000
                              select x)
                              .Union(from x in user
                                     join y in userdetails
                                     on x.Id equals y.Id
                                     where x.Id >= 10000
                                     select x);

            var linq = from x in user
                       join y in unionRight
                       on x.Id equals y.Id
                       where y.Mallagid > 0
                       orderby y.Mallagid
                       orderby x.Id
                       orderby y.Id
                       select new
                       {
                           x.Id,
                           y.Mallagid
                       };

            var results = linq.ToList();
        }

        [TestMethod]
        public void JoinInUnoinTest()
        {
            var user = new UserRepository(context);
            var userdetails = new UserDetailsRepository(context);

            var linq = (from x in user
                        join y in userdetails
                        on x.Id equals y.Id
                        where x.Id < 10000
                        select new
                        {
                            x.Id,
                            x.Bcid
                        })
                        .Union(from x in user
                               join y in userdetails
                               on x.Id equals y.Id
                               where x.Id < 10000
                               select new
                               {
                                   x.Id,
                                   x.Bcid
                               });

            var results = linq.ToList();
        }

        [TestMethod]
        public void UnoinSelectInJoinTest()
        {
            var user = new UserRepository(context);
            var userdetails = new UserDetailsRepository(context);

            var unionRight = (from x in user
                              join y in userdetails
                              on x.Id equals y.Id
                              where x.Id < 10000
                              select new
                              {
                                  x.Id,
                                  x.Bcid,
                                  x.Mallagid
                              })
                              .Union(from x in user
                                     join y in userdetails
                                     on x.Id equals y.Id
                                     where x.Id >= 10000
                                     select new
                                     {
                                         x.Id,
                                         x.Bcid,
                                         x.Mallagid
                                     });

            var linq = from x in user
                       join y in unionRight
                       on x.Id equals y.Id
                       where y.Mallagid > 0
                       orderby y.Mallagid
                       orderby x.Id
                       select new
                       {
                           x.Id,
                           y.Mallagid
                       };

            var results = linq.ToList();
        }

        [TestMethod]
        public void SelectInJoinTest()
        {
            var user = new UserRepository(context);
            var userdetails = new UserDetailsRepository(context);

            var linq = from x in userdetails
                       join y in user.Where(y => y.Bcid > 0)
                               .Select(y => new
                               {
                                   y.Id,
                                   y.Mallagid
                               })
                       on x.Id equals y.Id
                       where y.Mallagid > 0
                       orderby y.Mallagid
                       orderby x.Id
                       select new
                       {
                           x.Id,
                           y.Mallagid
                       };


            var results = linq.ToList();
        }

        [TestMethod]
        public void JoinSelectInJoinTest()
        {
            var user = new UserRepository(context);
            var userdetails = new UserDetailsRepository(context);

            var joinRight = from x in user
                            join y in userdetails
                            on x.Id equals y.Id
                            where x.Id > 0 && x.Id < 10000
                            select new
                            {
                                x.Id,
                                x.Mallagid
                            };

            var linq = from x in userdetails
                       join y in joinRight
                       on x.Id equals y.Id
                       where y.Mallagid > 0
                       orderby y.Mallagid
                       orderby x.Id
                       select new
                       {
                           x.Id,
                           y.Mallagid
                       };


            var results = linq.ToList();
        }

        [TestMethod]
        public void UnoinWhereInJoinTest()
        {
            var user = new UserRepository(context);
            var userdetails = new UserDetailsRepository(context);

            var unionRight = (from x in user
                              join y in userdetails
                              on x.Id equals y.Id
                              where x.Id < 10000
                              select x)
                              .Union(from x in user
                                     join y in userdetails
                                     on x.Id equals y.Id
                                     where x.Id >= 10000
                                     select x);

            var linq = from x in user
                       join y in unionRight.Where(y => y.Mallagid > 0)
                       on x.Id equals y.Id
                       where y.Mallagid > 0
                       orderby y.Mallagid
                       orderby x.Id
                       orderby y.Id
                       select new
                       {
                           x.Id,
                           y.Mallagid
                       };

            var results = linq.ToList();
        }

        [TestMethod]
        public void UnoinSelectWhereInJoinTest()
        {
            var user = new UserRepository(context);
            var userdetails = new UserDetailsRepository(context);

            var unionRight = (from x in user
                              join y in userdetails
                              on x.Id equals y.Id
                              where x.Id < 10000
                              select new
                              {
                                  x.Id,
                                  x.Bcid,
                                  x.Mallagid
                              })
                              .Union(from x in user
                                     join y in userdetails
                                     on x.Id equals y.Id
                                     where x.Id >= 10000
                                     select new
                                     {
                                         x.Id,
                                         x.Bcid,
                                         x.Mallagid
                                     });

            var linq = from x in user
                       join y in unionRight.Where(z => z.Bcid > 0)
                       on x.Id equals y.Id
                       where y.Mallagid > 0
                       orderby y.Mallagid
                       orderby x.Id
                       select new
                       {
                           x.Id,
                           y.Mallagid
                       };

            var results = linq.ToList();
        }

        [TestMethod]
        public void EqualsTest()
        {
            var userdetails = new UserDetailsRepository(context);

            var results = userdetails
                .Where(x => x.Id > 100)
                .Where(x => x.Id.Equals(101))
                .ToList();
        }

        private class IE
        {
            public int Id { get; set; }

            [Naming("Username")]
            public string Name { get; set; }
        }

        [TestMethod]
        public void MapTest()
        {
            var y = 100;
            var user = new UserRepository(context);
            var result = user.Where(x => x.Id > 0 && x.Id < y).Map<IE>();

            var list = result.ToList();
        }

        [TestMethod]
        public void SingleContextWithMultiTaskTest()
        {
            var user = new UserRepository(context);

            var tasks = new Task[100];

            for (int i = 0; i < 100; i++)
            {
                tasks[i] = TaskTest(user);
            }

            Task.WaitAll(tasks);
        }

        private Task TaskTest(UserRepository users)
        {
            return users.FirstOrDefaultAsync();
        }
    }
}

using CodeArts;
using CodeArts.Casting;
using CodeArts.Db;
using CodeArts.Db.Domain;
using CodeArts.Db.Exceptions;
using CodeArts.Db.Lts;
using CodeArts.Db.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.LinqAsync;
using System.Linq.Expressions;
using System.Text;
using UnitTest.Domain.Entities;
using UnitTest.Dtos;
using System.Transactions;
using System.Threading.Tasks;
using CodeArts.Db.Lts.Tests.Domain.Entities;

namespace UnitTest
{
    public class GroupByTest
    {
        public FeiUsers Key { get; set; }

        public DateTime CreatedTime { get; set; }
    }

    [TestClass]
    public class SqlServerTest
    {
        private static bool isCompleted;

        [TestInitialize]
        public void Initialize()
        {
            var adapter = new SqlServerLtsAdapter
            {
                MaxPoolSize = 120
            };

            DbConnectionManager.RegisterAdapter(adapter);
            DbConnectionManager.RegisterProvider<CodeArtsProvider>();

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
            var user = new UserRepository();

            var result = user.Select(x => new { x.Id, Enabled = true, OldId = x.Id + 1, OOID = ~y });//.Where(x => x.Id > 0 && x.Id < y);

            var list = result.ToList();

        }

        [TestMethod]
        public void SelectTest2()
        {
            var y = 100;
            bool enabled = true;

            var user = new UserRepository();

            var result = user.Select(x => new { x.Id, Enabled = enabled, OldId = x.Id + 1, OOID = y ^ 5 });//.Where(x => x.Id > 0 && x.Id < y);

            var list = result.ToList();
        }

        [TestMethod]
        public void WhereTest()
        {
            var y = 100;
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y);

            var list = result.ToList();
        }

        [TestMethod]
        public void WhereTest2()
        {
            var y = 100;
            bool enabled = true;
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && enabled && x.Id < y);

            var list = result.ToList();
        }

        [TestMethod]
        public void WhereTest3()
        {
            var y = 100;
            bool enabled = false;
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && !enabled && x.Id < y);

            var list = result.ToList();
        }

        [TestMethod]
        public void WhereTest4()
        {
            var y = 100;
            bool enabled = true;
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && !enabled && x.Id < y);

            var list = result.ToList();
        }

        [TestMethod]
        public void WhereTest5()
        {
            var y = 100;
            bool enabled = false;
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && enabled && x.Id < y);

            var list = result.ToList();
        }

        [TestMethod]
        public void SelectWhereTest()
        {
            var y = 100;
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Username }).Cast<UserSimDto>();

            var list = result.ToList();
        }

        [TestMethod]
        public void SelectCastWaistWhereTest()
        {
            var y = 100;
            var user = new UserRepository();

            var result = user.Cast<UserSimDto>().Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Username });

            var list = result.ToList();
        }

        [TestMethod]
        public void SingleOrDefaultWithArg()
        {
            var y = 100;
            var user = new UserRepository();
            var result = user.SingleOrDefault(x => x.Id < y && x.Userstatus > 0);
        }

        [TestMethod]
        public void UnionTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, Name = x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Realname });

            var list = result.Union(result2).ToList();
        }

        [TestMethod]
        public void UnionCountTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, Name = x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Realname });

            var count = result.Union(result2).Count();
        }

        [TestMethod]
        public void AnyTest()
        {
            var user = new UserRepository();
            var has = user.Any(x => x.Id < 100);
        }

        [TestMethod]
        public void NestedAnyTest()
        {
            var user = new UserRepository();
            var has = user.Where(x => !user.Any(y => y.Id < 100)).ToList();
        }

        [TestMethod]
        public void AllTest()
        {
            var user = new UserRepository();
            var has = user.All(x => x.Id < 100);
        }

        [TestMethod]
        public void NestedAllTest()
        {
            var user = new UserRepository();
            var results = user.Where(x => user.All(y => y.Id < 100)).ToList();
        }

        [TestMethod]
        public void AvgTest()
        {
            var user = new UserRepository();
            var avg = user.Where(x => x.Id < 100).Average(x => x.Id);
        }

        [TestMethod]
        public void UnionMaxTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, Name = x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Realname });

            var max = result.Union(result2).Max();
        }

        [TestMethod]
        public void UnionMaxWithArgTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, Name = x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Realname });

            var max = result.Union(result2).Max(x => x.Id);
        }

        [TestMethod]
        public void UnionTakeTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Username = x.Realname });

            var list = result.Union(result2).Take(10).ToList();
        }

        [TestMethod]
        public void UnionTakeOrderByTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, Name = x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Realname });

            var list = result.Union(result2).Take(10).OrderBy(x => x.Id).ToList();
        }


        [TestMethod]
        public void UnionOrderByTest() //! 必须配合排序函数（OrderBy/OrderByDescending）使用
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, Name = x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Realname });

            var list = result.Union(result2).OrderBy(x => x.Id).ToList();
        }

        [TestMethod]
        public void UnionTakeSkipOrderByTest() //! 必须配合排序函数（OrderBy/OrderByDescending）使用
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, Name = x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Realname });

            var list = result.Union(result2).Take(10).Skip(100).OrderBy(x => x.Id).ToList();
        }

        [TestMethod]
        public void UnionSkipOrderByTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, Name = x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Realname });

            var list = result.Union(result2).Skip(10).OrderBy(x => x.Id).ToList();
        }

        [TestMethod]
        public void LikeTest()
        {
            var y = 100;
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("liu")).Select(x => new { x.Id, OldId = x.Id + 1, OOID = y });

            var list = result.ToList();
        }
        [TestMethod]
        public void INTest()
        {
            var y = 100;
            var user = new UserRepository();
            var arr = new List<string> { "1", "2" };
            var result = user.Where(x => x.Id > 0 && x.Id < y && arr.Contains(x.Username))
                .Select(x => new { x.Id, OldId = x.Id + 1, OOID = y });

            var list = result.ToList();
        }

        [TestMethod]
        public void INSQLTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && details.Select(z => z.Nickname).Contains(x.Username))
                .Select(x => new { x.Id, OldId = x.Id + 1, OOID = y });

            var list = result.ToList();
        }

        [TestMethod]
        public void INTest2()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var arr = new string[] { "1", "2" };
            var result = user.Where(x => x.Id > 0 && x.Id < y && arr.Any() && details.Any(z => z.Nickname == x.Username))
                .Select(x => new { x.Id, OldId = x.Id + 1, OOID = y });

            var list = result.ToList();
        }

        [TestMethod]
        public void AnyINTest()
        {
            var y = 100;
            var user = new UserRepository();
            var arr = new List<int> { 1, 10 };
            var result = user.Where(x => x.Id > 0 && x.Id < ~~~y && !!arr.Any(item => item == x.Id))
                .Select(x => new { x.Id, OldId = x.Id + 1, OOID = -y });

            var list = result.ToList();
        }

        [TestMethod]
        public void IIFTest()
        {
            var y = 100;
            string cc = null;
            var user = new UserRepository();

            var result = user.Where(x => x.Id > 0 && x.Id < y)
                .Select(x => new { Id = x.Id > 5000 ? x.Id : x.Id + 5000, OldId = x.Id + 1, OOID = y > 0 ? y : 100, DD = cc ?? string.Empty });

            var value = result.ToString();
            var list = result.ToList();
        }
        [TestMethod]
        public void PropertyTest()
        {
            string str = null;
            var b = true;
            var c = false;
            var user = new UserRepository();
            var result = user.Where(x => b == c && x.Id < 200 && x.Username.Contains(str) && x.CreatedTime < DateTime.Now)
                .Select(x => new { x = b, Id = x.Id > 100 ? x.Id : x.Id + 100, Name = x.Username, Time = DateTime.Now.Ticks, OldId = x.Id + 1, Date = x.CreatedTime });
            var list = result.ToList();
        }

        [TestMethod]
        public void OrderByTest()
        {
            var user = new UserRepository();
            var result = user.OrderBy(x => x.CreatedTime);
            var list = result.ToList();
        }
        [TestMethod]
        public void WhereOrderByTest()
        {
            var str = "1";
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 200 && x.Username.Contains(str) && x.CreatedTime < DateTime.Now)
                .OrderBy(x => x.CreatedTime);
            var list = result.ToList();
        }

        [TestMethod]
        public void WhereHasValueTest()
        {
            var str = "1";
            int? i = 1;
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 200 && x.Username.Contains(str) && (i.HasValue && x.Id > i.Value) && x.CreatedTime < DateTime.Now);

            result = result.Where(x => x.Mallagid == 2).Where(x => x.Userstatus.HasValue);

            var list = result.OrderBy(x => x.CreatedTime).ToList();
        }
        [TestMethod]
        public void OrderBySelect()
        {
            var str = "1";
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 200 && x.Username.Contains(str) && x.CreatedTime < DateTime.Now);
            var list = result.OrderBy(x => x.CreatedTime).ToList();
            var count = result.Count();
        }
        [TestMethod]
        public void ReverseTest() //! 必须配合排序函数（OrderBy/OrderByDescending）使用
        {
            var str = "1";
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 200 && x.Username.Contains(str) && x.CreatedTime < DateTime.Now)
                .OrderBy(x => x.CreatedTime)
                .Reverse() //! 所有排序都逆转（不论前后）
                .OrderByDescending(x => x.Bcid);
            var list = result.ToList();
        }
        [TestMethod]
        public void ExistsTest()
        {
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id < 200 && details.Distinct().Any(y => y.Id == x.Id && y.Id < 100))
                .OrderBy(x => x.CreatedTime)
                .Distinct()
                .Reverse()
                .OrderByDescending(x => x.Bcid);
            var list = result.ToList();
        }

        [TestMethod]
        public void ReplaceTest()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now)
                .Select(x => new { x.Id, Name = x.Username.Replace("180", "189"), Time = DateTime.Now.Ticks, OldId = x.Id + 1, Date = x.CreatedTime });
            var list = result.ToList();
        }

        [TestMethod]
        public void SubstringTest()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now)
                .Select(x => new { x.Id, Name = x.Username.Substring(3), Time = DateTime.Now.Ticks, OldId = x.Id + 1, Date = x.CreatedTime });
            var list = result.ToList();
        }

        [TestMethod]
        public void ToUpperLowerTest()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now)
                .Select(x => new { x.Id, Name = x.Username.ToUpper(), Time = DateTime.Now.Ticks, OldId = x.Id + 1, Date = x.CreatedTime });
            var list = result.ToList();
        }
        [TestMethod]
        public void TrimTest()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now)
                .Select(x => new { x.Id, Name = x.Username.ToUpper().Trim(), Time = DateTime.Now.Ticks, OldId = x.Id + 1, Date = x.CreatedTime });
            var list = result.ToList();
        }
        [TestMethod]
        public void IsNullOrEmpty()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now && !string.IsNullOrEmpty(x.Username))
                .Select(x => new { x.Id, Name = x.Username, Time = DateTime.Now.Ticks, OldId = x.Id + 1, Date = x.CreatedTime });
            var list = result.ToList();
        }
        [TestMethod]
        public void IndexOf()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 100 && x.CreatedTime < DateTime.Now && x.Username.IndexOf("m", 2, 2) > 1)
                .Select(x => new { x.Id, Name = x.Username, Time = DateTime.Now.Ticks, OldId = x.Id + 1, Date = x.CreatedTime });
            var list = result.ToList();
        }
        [TestMethod]
        public void MaxTest()
        {
            var user = new UserRepository();
            var result = user.Max();
        }
        [TestMethod]
        public void MaxWithTest()
        {
            var user = new UserRepository();
            var result = user.Max(x => x.Id);
        }

        [TestMethod]
        public void WhereMaxTest()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now).Max();
        }

        [TestMethod]
        public void DistinctTest()
        {
            var user = new UserRepository();
            var result = user.Distinct();
            var list = result.ToList();
        }

        [TestMethod]
        public void DistinctSelectTest()
        {
            var user = new UserRepository();
            var result = user.Distinct().Select(x => x.Id);
            var list = result.ToList();
        }

        [TestMethod]
        public void DistinctWhereTest()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now).Distinct();
            var list = result.ToList();
        }

        [TestMethod]
        public void JoinTest()
        {
            var y = 100;
            var str = "1";
            var user = new UserRepository();
            var details = new UserDetailsRepository();
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
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var userWx = new UserWeChatRepository();
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
            FeiUserWeChatStatusEnum? status = FeiUserWeChatStatusEnum.Enabled;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var userWx = new UserWeChatRepository();
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
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var userWx = new UserWeChatRepository();
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
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = from x in user
                         join y in details on x.Id equals y.Id
                         where x.Id > 0 && y.Id < id && x.Username.Contains(str)
                         select new { x.Id, y.Nickname };
            var list = result.Count();
        }

        [TestMethod]
        public void JoinAnonymousEqaulsTest2()
        {
            var id = 100;
            var str = "1";
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = from x in user
                         join y in details
                         on new { x.Id, x.Username } equals new { y.Id, Username = y.Realname }
                         where x.Id > 0 && y.Id < id && x.Username.Contains(str)
                         select new { x.Id, y.Nickname };

            var list = result.ToList();
        }

        [TestMethod]
        public void JoinClassEqaulsTest2()
        {
            var id = 100;
            var str = "1";
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = from x in user
                         join y in details
                         on new FeiUsers { Id = x.Id, Username = x.Username } equals new FeiUsers { Id = y.Id, Username = y.Realname }
                         where x.Id > 0 && y.Id < id && x.Username.Contains(str)
                         select new { x.Id, y.Nickname };

            var list = result.ToList();
        }

        [TestMethod]
        public void JoinIntoTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = from x in user
                         join d in details on x.Id equals d.Id
                         into r
                         from c in r
                         where x.Id > 0 && c.Id < y
                         orderby x.Id, c.Registertime descending
                         select new { x.Id, OldId = x.Id + 1, OOID = c.Id, DDD = y };

            var list = result.ToList();

            /***
             * SELECT [x].[uid] AS [Id],([x].[uid] + 1) AS [OldId],[c].[uid] AS [OOID],@y AS [DDD] 
             *  FROM [fei_users] [x] 
             *  INNER JOIN [fei_userdetails] [c] ON [x].[uid] = [c].[uid] 
             * WHERE (([x].[uid] > 0) AND ([c].[uid] < @y)) 
             * ORDER BY [x].[uid],[c].[registertime] DESC
             */
        }


        [TestMethod]
        public void LeftJoinTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = from x in user
                         join d in details on x.Id equals d.Id
                         into r
                         from c in r.DefaultIfEmpty()
                         where x.Id > 0 && c.Id < y
                         orderby x.Id, c.Registertime descending
                         select new { x.Id, OldId = x.Id + 1, OOID = c.Id, DDD = y };

            var list = result.ToList();

            /***
             * SELECT [x].[uid] AS [Id],([x].[uid] + 1) AS [OldId],[c].[uid] AS [OOID],@y AS [DDD] 
             *  FROM [fei_users] [x] 
             *  LEFT JOIN [fei_userdetails] [c] ON [x].[uid] = [c].[uid] 
             * WHERE (([x].[uid] > 0) AND ([c].[uid] < @y)) 
             * ORDER BY [x].[uid],[c].[registertime] DESC
             */
        }

        [TestMethod]
        public void LeftJoinDefaultConstantExpressionTest()
        {
            var y = 100;
            FeiUserdetails userdetails = new FeiUserdetails
            {
                Id = 100,
                Registertime = DateTime.Now
            };
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = from x in user
                         join d in details on x.Id equals d.Id
                         into r
                         from c in r.DefaultIfEmpty(userdetails)
                         where x.Id > 0 && c.Id < y
                         orderby x.Id, c.Registertime descending
                         select new { x.Id, OldId = x.Id + 1, OOID = c.Id, DDD = y };

            var list = result.ToList();
            //! 默认值不在查询条件中体现。
            /***
             * SELECT [x].[uid] AS [Id],([x].[uid] + 1) AS [OldId],(CASE WHEN [c].[uid] IS NOT NULL THEN [c].[uid] ELSE @id END) AS [OOID],@y AS [DDD] 
             *  FROM [fei_users] [x] 
             *  LEFT JOIN [fei_userdetails] [c] ON [x].[uid] = [c].[uid] 
             * WHERE (([x].[uid] > 0) AND ([c].[uid] < @y)) 
             * ORDER BY [x].[uid],(CASE WHEN [c].[uid] IS NOT NULL THEN [c].[registertime] ELSE @registertime END) DESC
             */
        }

        [TestMethod]
        public void LeftJoinDefaultNewExpressionTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = from x in user
                         join d in details on x.Id equals d.Id
                         into r
                         from c in r.DefaultIfEmpty(new FeiUserdetails
                         {
                             Id = x.Id,
                             Registertime = x.CreatedTime
                         })
                         where x.Id > 0 && c.Id < y
                         orderby x.Id, c.Registertime descending
                         select new { x.Id, OldId = x.Id + 1, OOID = c.Id, DDD = y };

            var list = result.ToList();
            //! 默认值不在查询条件中体现。
            /***
             * SELECT [x].[uid] AS [Id],([x].[uid] + 1) AS [OldId],(CASE WHEN [c].[uid] IS NOT NULL THEN [c].[uid] ELSE [x].[uid] END) AS [OOID],@y AS [DDD] 
             *  FROM [fei_users] [x] 
             *  LEFT JOIN [fei_userdetails] [c] ON [x].[uid] = [c].[uid] 
             * WHERE (([x].[uid] > 0) AND ([c].[uid] < @y)) 
             * ORDER BY [x].[uid],(CASE WHEN [c].[uid] IS NOT NULL THEN [c].[registertime] ELSE [x].[created_time] END) DESC
             */
        }

        [TestMethod]
        public void LeftJoinGroupDefaultNewExpressionTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = from x in user
                         join d in details on x.Id equals d.Id
                         into r
                         from c in r.DefaultIfEmpty(new FeiUserdetails
                         {
                             Id = x.Id,
                             Registertime = x.CreatedTime
                         })
                         where x.Id > 0 && c.Id < y
                         group x by new { x.Id, x.Bcid, GroupId = c.Id, c.Registertime, x.CreatedTime } into g
                         orderby g.Key.Registertime descending
                         select new { g.Key };


            var list = result.ToList();
            //! 默认值不在查询条件中体现，注意字段加入分组时，默认值的表达式字段也需要加入分组。
            /***
             * SELECT [x].[uid] AS [__key____id],[x].[bcid] AS [__key____bcid],(CASE WHEN [c].[uid] IS NOT NULL THEN [c].[uid] ELSE [x].[uid] END) AS [__key____groupid],(CASE WHEN [c].[uid] IS NOT NULL THEN [c].[registertime] ELSE [x].[created_time] END) AS [__key____registertime],[x].[created_time] AS [__key____createdtime] 
             *  FROM [fei_users] [x] 
             *  LEFT JOIN [fei_userdetails] [c] ON [x].[uid] = [c].[uid] 
             * WHERE (([x].[uid] > 0) AND ([c].[uid] < @y)) 
             * GROUP BY [x].[uid],[x].[bcid],(CASE WHEN [c].[uid] IS NOT NULL THEN [c].[uid] ELSE [x].[uid] END),(CASE WHEN [c].[uid] IS NOT NULL THEN [c].[registertime] ELSE [x].[created_time] END),[x].[created_time] 
             * ORDER BY (CASE WHEN [c].[uid] IS NOT NULL THEN [c].[registertime] ELSE [x].[created_time] END) DESC
             */
        }

        [TestMethod]
        public void TakeTest()
        {
            var user = new UserRepository();
            var result = user.From(x => x.TableName).Take(10);
            var list = result.ToList();
        }
        [TestMethod]
        public void TakeWhereTest()
        {
            var user = new UserRepository();
            var result = user.Take(10).Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now);
            var list = result.ToList();
        }

        [TestMethod]
        public void SkipOrderByTest()
        {
            var user = new UserRepository();
            var result = user.Skip(124680).OrderBy(x => x.Id);
            var list = result.ToList();
        }
        [TestMethod]
        public void TakeSkipOrderByTest()
        {
            var user = new UserRepository();
            var result = user.Take(10).Skip(10000).OrderBy(x => x.Id);
            var list = result.ToList();
        }
        [TestMethod]
        public void FirstOrDefaultTest()
        {

            var user = new UserRepository();
            var userEntity = user.FirstOrDefault();
        }
        [TestMethod]
        public void FirstOrDefaultWhereTest()
        {
            var user = new UserRepository();

            var userEntity = user.Where(x => x.Username.Length > 10 && x.Id < 100 && x.CreatedTime < DateTime.Now)
                .OrderBy(x => x.Id)
                .FirstOrDefault();
        }

        [TestMethod]
        public void LengthTest()
        {
            var user = new UserRepository();

            var userEntity = user.Where(x => x.Username.Length > 10)
                .OrderBy(x => x.Id)
                .FirstOrDefault();
        }

        [TestMethod]
        public void HasValueTest()
        {
            var userdetails = new UserRepository();

            var results = userdetails
                .Where(x => x.Userstatus.HasValue)
                .ToList();
        }

        [TestMethod]
        public void ValueTest()
        {
            var userdetails = new UserRepository();

            var results = userdetails
                .Where(x => x.Userstatus.Value == 1)
                .ToList();
        }

#if NETSTANDARD2_1

        [TestMethod]
        public void TakeLastOrderByTest() //! 必须配合排序函数（OrderBy/OrderByDescending）使用
        {
            var user = new UserRepository();

            var results = user.TakeLast(10).OrderBy(x => x.CreatedTime).ToList();
        }


        [TestMethod]
        public void SkipLastOrderByTest()  //! 必须配合排序函数（OrderBy/OrderByDescending）使用
        {
            var user = new UserRepository();

            var results = user.OrderBy(x => x.CreatedTime).SkipLast(124680).ToList();
        }
#endif
        [TestMethod]
        public void TakeWhileTest()
        {
            var str = "1";
            var user = new UserRepository();
            var results = user.TakeWhile(x => x.Id < 200 && x.Username.Contains(str) && x.CreatedTime < DateTime.Now).Take(10).ToList();
        }

        [TestMethod]
        public void SkipWhileTest()
        {
            var str = "1";
            var user = new UserRepository();
            var results = user.SkipWhile(x => x.Id < 200 && x.Username.Contains(str) && x.CreatedTime < DateTime.Now).Take(10).ToList();
        }
        [TestMethod]
        public void CastTest() //? 在SQL中，只会生成共有的属性(不区分大小写)。
        {
            var y = 100;
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y).Cast<UserSimDto>();

            var list = result.ToList();
        }
        [TestMethod]
        public void CastCountTest() //? 在SQL中，只会生成共有的属性(不区分大小写)。
        {
            var y = 100;
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y).Cast<UserSimDto>();

            var list = result.Count();
        }
        [TestMethod]
        public void UnionCastTakeTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Username = x.Realname });

            var list = result.Union(result2).Cast<UserSimDto>().Take(10).ToList();
        }
        [TestMethod]
        public void UnionCastCountTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Username = x.Realname });

            var Count = result.Union(result2).Count();
        }

        [TestMethod]
        public void IntersectTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Username = x.Realname });

            var list = result.Intersect(result2).Cast<UserSimDto>().Take(10).ToList();

        }
        [TestMethod]
        public void IntersectCountTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Username = x.Realname });

            var Count = result.Intersect(result2).Count();
        }

        [TestMethod]
        public void NullableWhereTest() //? 当条件的一端为Null，且另一端为值类型时，条件会被自动忽略。
        {
            var y = 100;

            DateTime? date = null;

            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.CreatedTime > date)
                .Select(x => new { x.Id, Name = x.Username }).Cast<UserSimDto>();

            var list = result.ToList();
        }

        [TestMethod]
        public void BitOperationTest()
        {
            var user = new UserRepository();

            var result = user.Where(x => (x.Userstatus & 1) == 1 && x.Id < 100).Take(10);
            var list = result.ToList();
        }

        [TestMethod]
        public void CountWithArgumentsTest()
        {

            var user = new UserRepository();

            var count = user.Count(x => x.Mallagid == 2);
        }

        [TestMethod]
        public void CustomFirstWithMethodTest()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id > user.Skip(100000).OrderBy(y => y.CreatedTime).Select(y => y.Id).First() && x.CreatedTime < DateTime.Now)
                .OrderBy(x => x.CreatedTime)
                .Take(10)
                .Skip(100)
                .Select(x => x.Username.Substring(2))
                .ToList();
        }

        [TestMethod]
        public void RequiredTest()
        {
            var user = new UserRepository();

            try
            {
                user.Where(x => x.Id < 0)
                .DefaultIfEmpty(new FeiUsers
                {
                    Id = 50,
                    Bcid = 1,
                    CreatedTime = DateTime.Now
                })
                .First();
            }
            catch (DRequiredException)
            {
                //? 查询结果不加【OrDefault】后缀时，数据库未查询到数据，ORM会抛出【DRequiredException】异常。
            }
        }

        //? 不经过SQL处理，仅代码逻辑处理。
        [TestMethod]
        public void DefaultIfEmptyTest()
        {
            var user = new UserRepository();

            var result = user.Where(x => x.Id < 100)
                 .DefaultIfEmpty(new FeiUsers
                 {
                     Id = 50,
                     Bcid = 1,
                     CreatedTime = DateTime.Now
                 })
                 .FirstOrDefault();
        }

        [TestMethod]
        public void InsertTest()
        {

            var user = new UserRepository();

            var entry = new FeiUsers
            {
                Bcid = 0,
                Username = "admin",
                Userstatus = 1,
                Mobile = "18980861011",
                Email = "tinylit@foxmail.com",
                Password = "123456",
                Salt = string.Empty,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };

            var i = user.AsInsertable(entry).ExecuteCommand();
        }

        [TestMethod]
        public void UpdateTest()
        {
            var user = new UserRepository();

            var entry = new FeiUsers
            {
                Bcid = 0,
                Userstatus = 1,
                Mobile = "18980861011",
                Email = "tinylit@foxmail.com",
                Password = "123456",
                Salt = string.Empty,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };

            var i = user.AsUpdateable(entry)
                .UseTransaction(System.Data.IsolationLevel.ReadCommitted)
                .ExecuteCommand();

            var j = user.AsUpdateable(entry)
                .Limit(x => x.Password)
                .Where(x => x.Username ?? x.Mobile)
                .ExecuteCommand();

            var k = user
                    .From(x => x.TableName)
                    .Where(x => x.Username == "admin")
                    .Update(x => new FeiUsers
                    {
                        Mallagid = 2,
                        Username = x.Username.Substring(0, 4)
                    });

        }

        [TestMethod]
        public void DeleteTest()
        {
            var user = new UserRepository();

            var entry = new FeiUsers
            {
                Bcid = 0,
                Username = "admi",
                Userstatus = 1,
                Mobile = "18980861011",
                Email = "tinylit@foxmail.com",
                Password = "123456",
                Salt = string.Empty,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };

            var x0 = user.AsDeleteable(entry)
                .ExecuteCommand();

            var list = new List<FeiUsers>();

            for (int i = 0; i < 1000; i++)
            {
                list.Add(entry);
            }

            var x1 = user.AsDeleteable(list)
                .Where(x => x.Username)
                .ExecuteCommand(120);

            var x2 = user.Delete(x => x.Username == "admi");
        }

        [TestMethod]
        public void TimeOutTest()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < 20).TimeOut(10);

            var results = result.ToList();
        }

        [TestMethod]
        public void TimeOut2Test()
        {
            var user = new UserRepository();
            var result = user
                .From(x => x.TableName)
                .Where(x => x.Username == "admin")
                .TimeOut(10)
                .Update(x => new FeiUsers
                {
                    Username = x.Username.Substring(0, 4)
                });
        }

        [TestMethod]
        public void MissingErrorTest()
        {
            var user = new UserRepository();

            string errMsg = "未查询到用户信息!";

            try
            {
                var userEntry = user.Where(x => x.Id > 10 && x.Id < 10)
                    .NoResultError(errMsg)
                    .First();

                Assert.Fail();
            }
            catch (DRequiredException e)
            {
                Assert.IsTrue(errMsg == e.Message);
            }
        }

        [TestMethod]
        public void AnyWhere()
        {
            var user = new UserRepository();
            var expression = ExpressionSplicing.True<FeiUsers>();
            var userdetails = new UserDetailsRepository();

            var results = userdetails
                .Where(x => x.Id > 100)
                .Where(x => !user.Where(expression).Any(y => x.Id == y.Id))
                .ToList();
        }

        [TestMethod]
        public void JoinInJoinTest()
        {
            var user = new UserRepository();
            var userdetails = new UserDetailsRepository();

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
            var user = new UserRepository();
            var userdetails = new UserDetailsRepository();

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
            var user = new UserRepository();
            var userdetails = new UserDetailsRepository();

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
            var user = new UserRepository();
            var userdetails = new UserDetailsRepository();

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
            var user = new UserRepository();
            var userdetails = new UserDetailsRepository();

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
            var user = new UserRepository();
            var userdetails = new UserDetailsRepository();

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
            var user = new UserRepository();
            var userdetails = new UserDetailsRepository();

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
            var user = new UserRepository();
            var userdetails = new UserDetailsRepository();

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
        public void FromTest()
        {
            var user = new UserRepository();
            var userdetails = new UserDetailsRepository();

            var results = userdetails
                .From(x => x.TableName) //? 指定查询表（数据分表）。
                .Where(x => x.Id > 100)
                .Where(x => user.Any(y => x.Id == y.Id))
                .ToList();
        }

        [TestMethod]
        public void EqualsTest()
        {
            var user = new UserRepository();
            var userdetails = new UserDetailsRepository();

            var results = userdetails
                .From(x => x.TableName) //? 指定查询表（数据分表）。
                .Where(x => x.Id > 100)
                .Where(x => user.Any(y => x.Id.Equals(y.Id)))
                .ToList();
        }

        [TestMethod]
        public void FromJoinTest()
        {
            var y = 100;
            var str = "1";
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = from x in user.From(x => x.TableName) //? 指定查询表（数据分表）。
                         join d in details on x.Id equals d.Id
                         where x.Id > 0 && d.Id < y && x.Username.Contains(str)
                         orderby x.Id, d.Registertime descending
                         select new { x.Id, OldId = x.Id + 1, OOID = d.Id, DDD = y };

            var list = result.ToList();
        }

        [TestMethod]
        public void SqlCaptureTest()
        {
            var user = new UserRepository();
            var userdetails = new UserDetailsRepository();

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

            using (var c = new SqlCapture
            {
                Captured = context =>
                {
                    Debug.WriteLine(context.Sql);
                }
            })
            {
                var results = linq.ToList();
            }
        }

        [TestMethod]
        public void SqlCaptureTest2()
        {
            var user = new UserRepository();
            var userdetails = new UserDetailsRepository();

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

            using (var c = new SqlCapture
            {
                Captured = context =>
                {
                    Debug.WriteLine($"1:{context.Sql}");
                }
            })
            {
                var results = userdetails.Where(x => x.Id > 100)
                 .Where(x => user.Any(y => x.Id == y.Id))
                 .ToList();

                using (var c2 = new SqlCapture
                {
                    Captured = context =>
                    {
                        Debug.WriteLine($"2:{context.Sql}");
                    }
                })
                {
                    var results2 = linq.ToList();
                }
            }
        }

        /// <summary>
        /// 表达式拼接测试。
        /// </summary>
        [TestMethod]
        public void ExpressionSplicingTest()
        {
            var y = 100;
            var user = new UserRepository();
            var expression = ExpressionSplicing.True<FeiUsers>();

            expression = expression.And(x => x.Id > 0);

            expression = expression.And(z => z.Id < y);

            var result = user.Where(expression);

            var list = result.ToList();
        }

        /// <summary>
        /// 表达式拼接测试。
        /// </summary>
        [TestMethod]
        public void ExpressionSplicingEmptyTest()
        {
            var y = 100;
            var user = new UserRepository();
            var expression = ExpressionSplicing.True<FeiUsers>();

            var result = user.Where(x => x.Id > 0 && x.Id < y).Where(expression);

            var list = result.ToList();
        }

        /// <summary>
        /// 事务异步测试。
        /// </summary>
        [TestMethod]
        public void UpdateAsyncTest()
        {
            Task.WaitAll(Aw_UpdateAsyncTest());
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupBySingleFieldTest()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by x.Mobile into g
                       where g.Key == ""
                       orderby g.Count()
                       select new
                       {
                           g.Key,
                           Count = g.Count()
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldTest()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new FeiUsers { Mobile = x.Mobile, Bcid = x.Bcid } into g
                       where g.Key.Mobile == ""
                       orderby g.Count()
                       select new
                       {
                           g.Key.Mobile,
                           Count = g.Count()
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldCountWhereTest()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new FeiUsers { Mobile = x.Mobile, Bcid = x.Bcid } into g
                       where g.Key.Mobile == ""
                       orderby g.Count()
                       select new
                       {
                           g.Key.Mobile,
                           Count = g.Count(),
                           Count1 = g.Where(x => x.Email != null).Count(),
                           Count2 = g.Count(x => x.Bcid > 0),
                           Count3 = g.Where(x => x.Email != null).Count(x => x.Bcid > 0)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldCountWhereTest2()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new FeiUsers { Mobile = x.Mobile, Bcid = x.Bcid } into g
                       where g.Key.Mobile == "" && g.Key.Bcid > 0
                       orderby g.Count()
                       select new
                       {
                           g.Key.Mobile,
                           Count = g.Count(),
                           Count1 = g.Where(x => x.Email != null).Count(),
                           Count2 = g.Count(x => x.Bcid > 0),
                           Count3 = g.Where(x => x.Email != null).Count(x => x.Bcid > 0)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldCountWhereTest3()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new FeiUsers { Mobile = x.Mobile, Bcid = x.Bcid } into g
                       where g.Key.Mobile == "" && g.Count(x => x.Bcid > 0) > 2
                       orderby g.Count()
                       select new
                       {
                           g.Key.Mobile,
                           Count = g.Count(),
                           Count1 = g.Where(x => x.Email != null).Count(),
                           Count2 = g.Count(x => x.Bcid > 0),
                           Count3 = g.Where(x => x.Email != null).Count(x => x.Bcid > 0)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldCountWhereTest4()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new FeiUsers { Mobile = x.Mobile, Bcid = x.Bcid } into g
                       where g.Key.Mobile == ""
                       orderby g.Count()
                       select new
                       {
                           g.Key.Mobile,
                           Any = g.Any(),
                           Any2 = g.Where(x => x.Email != null).Any(),
                           Any3 = g.Any(x => x.Bcid > 0),
                           Any4 = g.Where(x => x.Email != null).Any(x => x.Bcid > 0)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldCountWhereTest5()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new FeiUsers { Mobile = x.Mobile, Bcid = x.Bcid } into g
                       where g.Key.Mobile == ""
                       orderby g.Count()
                       select new
                       {
                           g.Key.Mobile,
                           All = g.All(x => x.Bcid > 0),
                           All2 = g.Where(x => x.Email != null).All(x => x.Bcid > 0)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldCountWhereTest6()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new FeiUsers { Mobile = x.Mobile, Bcid = x.Bcid } into g
                       where g.Key.Mobile == ""
                       orderby g.Count()
                       select new
                       {
                           g.Key.Mobile,
                           Sum = g.Sum(x => x.Bcid),
                           Sum2 = g.Where(x => x.Email != null).Sum(x => x.Bcid)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldCountWhereTest7()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new FeiUsers { Mobile = x.Mobile, Bcid = x.Bcid } into g
                       where g.Key.Mobile == ""
                       orderby g.Count()
                       select new
                       {
                           g.Key.Mobile,
                           Average = g.Average(x => x.Bcid),
                           Average2 = g.Where(x => x.Bcid > 0).Average(x => x.Bcid)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldCountWhereTest8()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new FeiUsers { Mobile = x.Mobile, Bcid = x.Bcid } into g
                       where g.Key.Mobile == ""
                       orderby g.Count()
                       select new
                       {
                           g.Key.Mobile,
                           Max = g.Max(x => x.Bcid),
                           Max2 = g.Where(x => x.Bcid > 0).Max(x => x.Bcid)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldAnonymousTest()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new { x.Mobile, x.Bcid } into g
                       where g.Key.Mobile == ""
                       orderby g.Count()
                       select new
                       {
                           g.Key.Mobile,
                           CreatedTime = g.Max(x => x.CreatedTime)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByOrderByKeyTest()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new { x.Mobile, x.Bcid } into g
                       where g.Key.Mobile == ""
                       orderby g.Key
                       select new
                       {
                           g.Key.Mobile,
                           CreatedTime = g.Max(x => x.CreatedTime)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByOrderByKeyPropertyTest()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new { x.Mobile, x.Bcid } into g
                       where g.Key.Mobile == ""
                       orderby g.Key.Mobile
                       select new
                       {
                           g.Key.Mobile,
                           CreatedTime = g.Max(x => x.CreatedTime)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldAnonymousKeyPropertyTest()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new { x.Mobile, x.Bcid } into g
                       where g.Key.Mobile == ""
                       orderby g.Count()
                       select new
                       {
                           g.Key,
                           CreatedTime = g.Max(x => x.CreatedTime)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldClassKeyPropertyTest()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new FeiUsers { Mobile = x.Mobile, Bcid = x.Bcid } into g
                       where g.Key.Mobile == ""
                       orderby g.Count()
                       select new
                       {
                           g.Key,
                           CreatedTime = g.Max(x => x.CreatedTime)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldClassKeyWithClassPropertyTest()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new FeiUsers { Mobile = x.Mobile, Bcid = x.Bcid } into g
                       where g.Key.Mobile == ""
                       orderby g.Count()
                       select new GroupByTest
                       {
                           Key = g.Key,
                           CreatedTime = g.Max(x => x.CreatedTime)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldAnonymousHavingKeyTest()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new { x.Mobile, x.Bcid } into g
                       where g.Key == new { Mobile = string.Empty, Bcid = 0 }
                       orderby g.Count()
                       select new
                       {
                           g.Key,
                           CreatedTime = g.Max(x => x.CreatedTime)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 分组测试。
        /// </summary>
        [TestMethod]
        public void GroupByMultiFieldClassHavingKeyTest()
        {
            var user = new UserRepository();

            var linq = from x in user
                       where x.Id > 0
                       group x by new FeiUsers { Mobile = x.Mobile, Bcid = x.Bcid } into g
                       where g.Key != new FeiUsers { Mobile = string.Empty, Bcid = 0 }
                       orderby g.Count()
                       select new
                       {
                           g.Key,
                           CreatedTime = g.Max(x => x.CreatedTime)
                       };

            var results = linq.ToList();
        }

        /// <summary>
        /// 左空转IS NULL。
        /// </summary>
        [TestMethod]
        public void WhereLeftNull()
        {
            var userdetails = new UserRepository();

            var results = userdetails
                .Where(x => null == x.Userstatus)
                .ToList();
        }

        /// <summary>
        /// 右空转IS NOT NULL。
        /// </summary>
        [TestMethod]
        public void WhereRightNull()
        {
            var userdetails = new UserRepository();

            var results = userdetails
                .Where(x => x.Userstatus != null)
                .ToList();
        }

        /// <summary>
        /// 左空合并运算符。
        /// </summary>
        [TestMethod]
        public void WhereLeftCoalesce()
        {
            var userdetails = new UserRepository();

            int? value = null;

            var results = userdetails
                .Where(x => (x.Userstatus ?? value ?? 1) == 1)
                .ToList();
        }


        /// <summary>
        /// 右可空合并运算符。
        /// </summary>

        [TestMethod]
        public void WhereRightCoalesce()
        {
            int? value = null;

            var userdetails = new UserRepository();

            var results = userdetails
                .Where(x => 1 == (x.Userstatus ?? value ?? 1))
                .ToList();
        }

        /// <summary>
        /// 两端可空合并运算符。
        /// </summary>
        [TestMethod]
        public void WhereDoubleCoalesce()
        {
            int? value = null;

            var userdetails = new UserRepository();

            var results = userdetails
                .Where(x => (x.Userstatus ?? value ?? 1) == (value ?? 1))
                .ToList();
        }

        private async Task Aw_UpdateAsyncTest()
        {
            var user = new UserRepository();

            var entry = new FeiUsers
            {
                Bcid = 0,
                Userstatus = 1,
                Mobile = "18980861011",
                Email = "tinylit@foxmail.com",
                Password = "123456",
                Salt = string.Empty,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };

            var i = await user.AsUpdateable(entry)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 事务异步测试。
        /// </summary>
        [TestMethod]
        public void TransactionAsyncTest()
        {
            var tasks = new Task[100];

            for (int i = 0; i < 100; i++)
            {
                tasks[i] = Aw_TransactionAsyncTest();
            }

            Task.WaitAll(tasks);
        }

        private async Task Aw_TransactionAsyncTest()
        {
            var user = new UserRepository();

            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var c2 = new SqlCapture
                {
                    Captured = context =>
                    {
                        Debug.WriteLine($"2:{context.Sql}");
                    }
                })
                {
                    var results = await user.ToListAsync();
                }
            }
        }

        [TestMethod]
        public void TestFirst()
        {
            var bucket = "hys-good-skus";

            var buckets = new DbRepository<OssBuckets>();

            var ossBucket = buckets
             .NoResultError($"未找到桶({bucket})的相关信息!")
             .First(x => x.Name == bucket && x.Enabled);
        }

        [TestMethod]
        public void TestFirst2()
        {
            var bucket = "hys-good-skus";

            var buckets = new DbRepository<OssBuckets>();

            var ossBucket = buckets
             .NoResultError($"未找到桶({bucket})的相关信息!")
             .First(x => x.Name == bucket && x.Enabled == true);
        }

        [TestMethod]
        public void TestFirst3()
        {
            var bucket = "hys-good-skus";

            var buckets = new DbRepository<OssBuckets>();

            var ossBucket = buckets
             .NoResultError($"未找到桶({bucket})的相关信息!")
             .First(x => x.Name == bucket && x.Enabled != false);
        }
    }
}

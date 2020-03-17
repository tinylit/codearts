using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeArts.ORM;
using CodeArts.ORM.Domain;
using CodeArts.ORM.Exceptions;
using CodeArts.ORM.MySql;
using CodeArts.ORM.SqlServer;
using CodeArts.ORM.Tests;
using CodeArts.SqlServer;
using CodeArts.SqlServer.Formatters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnitTest.Domain.Entities;
using UnitTest.Dtos;

namespace UnitTest
{
    [TestClass]
    public class SqlServerTest
    {
        private static bool isCompleted = true;

        [TestInitialize]
        public void Initialize()
        {
            var adapter = new SqlServerAdapter();
            DbConnectionManager.AddAdapter(adapter);
            DbConnectionManager.AddProvider<CodeArtsProvider>();

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

            var result = user.Select(x => new { x.Id, OldId = x.Id + 1, OOID = y });//.Where(x => x.Id > 0 && x.Id < y);

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
        public void SelectWhereTest()
        {
            var y = 100;
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Username }).Cast<UserSimDto>();

            var list = result.ToList();


            /**
             * SELECT [x].[uid] AS [Id] 
             * FROM [fei_users] [x] 
             * WHERE (([x].[uid] > @__variable_1) AND ([x].[uid] < @y)
             */
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
            /**
             * SELECT [uid] AS [Id], [username] AS [Name] 
             * FROM [fei_users] 
             * WHERE ((([uid] > @__variable_1) AND ([uid] < @y)) AND [username] LIKE @__variable_2) 
             * 
             * UNION 
             * 
             * SELECT [uid] AS [Id], [realname] AS [Name] 
             * FROM [fei_userdetails] 
             * WHERE (([uid] > @__variable_3) AND ([uid] < @y)) 
             */
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
            /**
             * SELECT [uid] AS [Id], [username] AS [Name] 
             * FROM [fei_users] 
             * WHERE ((([uid] > @__variable_1) AND ([uid] < @y)) AND [username] LIKE @__variable_2) 
             * UNION 
             * SELECT [uid] AS [Id], [realname] AS [Name] 
             * FROM [fei_userdetails] 
             * WHERE (([uid] > @__variable_3) AND ([uid] < @y))
             */
        }

        [TestMethod]
        public void AnyTest()
        {
            var user = new UserRepository();
            var has = user.Any(x => x.Id < 100);

            /**
             * SELECT CASE WHEN 
             *  EXISTS(SELECT [uid] AS [Id] FROM [fei_users] WHERE ([uid] < @__variable_1)) 
             * THEN 
             *  @__variable_true
             * ELSE 
             *  @__variable_false
             * END
             */
        }
        [TestMethod]
        public void AllTest()
        {
            var user = new UserRepository();
            var has = user.Where(x => x.Id < 100).All(x => x.Id < 100);

            /**
             * SELECT CASE WHEN 
             *  EXISTS(SELECT [x].[uid] AS [Id] FROM [fei_users] [x] WHERE ([x].[uid] < @__variable_1) AND ([x].[uid] < @__variable_2)) 
             *  
             *  AND 
             *  
             *  NOT EXISTS(SELECT [x].[uid] AS [Id] FROM [fei_users] [x] WHERE ([x].[uid] < @__variable_3) AND ([x].[uid] >= @__variable_4)) 
             * THEN 
             *  @__variable_true 
             * ELSE 
             *  @__variable_false 
             * END
             */
        }

        [TestMethod]
        public void AvgTest()
        {
            var user = new UserRepository();
            var has = user.Where(x => x.Id < 100).Average(x => x.Id);

            /**
             * SELECT Avg([x].[uid]) 
             * FROM [fei_users] [x] 
             * WHERE ([x].[uid] < @__variable_1)
             */
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
            /**
             * SELECT Max([Id]) AS [Id], Max([Name]) AS [Name] 
             * FROM (
             *      SELECT [uid] AS [Id], [username] AS [Name] 
             *      FROM [fei_users] 
             *      WHERE ((([uid] > @__variable_1) AND ([uid] < @y)) AND [username] LIKE @__variable_2) 
             *      
             *      UNION 
             *      
             *      SELECT [uid] AS [Id], [realname] AS [Name] 
             *      FROM [fei_userdetails] 
             *      WHERE (([uid] > @__variable_3) AND ([uid] < @y))
             * ) [CTE_UNION]
             */
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
            /**
             * SELECT Max([Id]) 
             * FROM (
             *      SELECT [uid] AS [Id], [username] AS [Name] 
             *      FROM [fei_users] 
             *      WHERE ((([uid] > @__variable_1) AND ([uid] < @y)) AND [username] LIKE @__variable_2) 
             *      
             *      UNION 
             *      
             *      SELECT [uid] AS [Id], [realname] AS [Name] 
             *      FROM [fei_userdetails] 
             *      WHERE (([uid] > @__variable_3) AND ([uid] < @y))
             * ) [CTE_UNION]
             */
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
            /**
             * SELECT 
             * TOP 10 [Id],[Name] 
             * FROM (
             *      SELECT [uid] AS [Id], [username] AS [Name] 
             *      FROM [fei_users] 
             *      WHERE ((([uid] > @__variable_1) AND ([uid] < @y)) AND [username] LIKE @__variable_2) 
             *      
             *      UNION 
             *      
             *      SELECT [uid] AS [Id], [realname] AS [Name] 
             *      FROM [fei_userdetails] 
             *      WHERE (([uid] > @__variable_3) AND ([uid] < @y))
             * ) [CTE]
             */
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
            /**
             * SELECT 
             * TOP 10 [Id],[Name] 
             * FROM (
             *      SELECT [uid] AS [Id], [username] AS [Name] 
             *      FROM [fei_users] 
             *      WHERE ((([uid] > @__variable_1) AND ([uid] < @y)) AND [username] LIKE @__variable_2) 
             *      
             *      UNION 
             *      
             *      SELECT [uid] AS [Id], [realname] AS [Name] 
             *      FROM [fei_userdetails] 
             *      WHERE (([uid] > @__variable_3) AND ([uid] < @y))
             * ) [CTE] 
             * ORDER BY [Id] DESC    
             */
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
            /**
             * SELECT [Id],[Name] 
             * FROM (
             *  SELECT ROW_NUMBER() OVER( ORDER BY [Id]) AS [__Row_number_],
             *  [Id],[Name] FROM (
             *      SELECT [x].[uid] AS [Id],[x].[username] AS [Name] 
             *      FROM [fei_users] [x] 
             *      WHERE ((([x].[uid]>@__variable_1) AND ([x].[uid]<@y)) AND [x].[username] LIKE @__variable_2) 
             *      
             *      UNION 
             *      
             *      SELECT [x].[uid] AS [Id],[x].[realname] AS [Name] 
             *      FROM [fei_userdetails] [x] 
             *      WHERE (([x].[uid]>@__variable_3) AND ([x].[uid]<@y))
             *  ) [CTE_ROW_NUMBER]
             * ) [CTE] 
             * WHERE [__Row_number_] > 100 AND [__Row_number_]<=110
             */
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
            /**
             * SELECT [Id],[Name] 
             * FROM (
             *  SELECT ROW_NUMBER() OVER( ORDER BY [Id]) AS [__Row_number_],
             *  [Id],[Name] FROM (
             *      SELECT [x].[uid] AS [Id],[x].[username] AS [Name] 
             *      FROM [fei_users] [x] 
             *      WHERE ((([x].[uid]>@__variable_1) AND ([x].[uid]<@y)) AND [x].[username] LIKE @__variable_2) 
             *      
             *      UNION 
             *      
             *      SELECT [x].[uid] AS [Id],[x].[realname] AS [Name] 
             *      FROM [fei_userdetails] [x] 
             *      WHERE (([x].[uid]>@__variable_3) AND ([x].[uid]<@y))
             *  ) [CTE_ROW_NUMBER]
             * ) [CTE] 
             * WHERE [__Row_number_] > 100 AND [__Row_number_]<=110
             */
        }

        [TestMethod]
        public void UnionSkipOrderByTest()
        {
            var y = 100;
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("admin")).Select(x => new { x.Id, Name = x.Username });

            var result2 = details.Where(x => x.Id > 0 && x.Id < y).Select(x => new { x.Id, Name = x.Realname });

            var list = result.Union(result2).Skip(124680).OrderBy(x => x.Id).ToList();
            /**
             * SELECT [Id],[Name] 
             * FROM (
             *      SELECT ROW_NUMBER() OVER(ORDER BY [Id] DESC) AS [__Row_number_],[Id],[Name] 
             *      FROM (
             *          SELECT [uid] AS [Id], [username] AS [Name] 
             *          FROM [fei_users] 
             *          WHERE ((([uid] > @__variable_1) AND ([uid] < @y)) AND [username] LIKE @__variable_2) 
             *          
             *          UNION 
             *          
             *          SELECT [uid] AS [Id], [realname] AS [Name] 
             *          FROM [fei_userdetails] 
             *          WHERE (([uid] > @__variable_3) AND ([uid] < @y))
             *      ) [CTE_ROW_NUMBER]
             * ) [CTE] 
             * WHERE [__Row_number_] > 124680
             */
        }

        [TestMethod]
        public void LikeTest()
        {
            var y = 100;
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y && x.Username.Contains("liu")).Select(x => new { x.Id, OldId = x.Id + 1, OOID = y });

            var list = result.ToList();
            /**
             * SELECT [uid] AS [Id], ([uid] + @__variable_1) AS [OldId], @y AS [OOID] FROM [fei_users] 
             * WHERE ((([uid] > @__variable_2) AND ([uid] < @y)) AND [username] LIKE @__variable_3)
             */
        }
        [TestMethod]
        public void INTest()
        {
            var y = 100;
            var user = new UserRepository();
            var arr = new List<string> { /*"1", "2" */};
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

            /**
             * SELECT [x].[uid] AS [Id],([x].[uid]+@__variable_2) AS [OldId],@y AS [OOID] 
             * FROM [fei_users] [x] 
             * WHERE ((([x].[uid]>@__variable_1) AND ([x].[uid]<@y)) 
             * AND [x].[username] IN(SELECT [z].[nickname] FROM [fei_userdetails] [z]))
             */
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
            /**
             * SELECT [x].[uid] AS [Id], ([x].[uid] + 1) AS [OldId], @y AS [OOID] 
             * FROM [fei_users] [x] 
             * WHERE ((([x].[uid] > 0) AND ([x].[uid] < @y)) AND [x].[username] IN ('1', '2'))
             */
        }
        [TestMethod]
        public void AnyINTest()
        {
            var y = 100;
            var user = new UserRepository();
            var arr = new List<int> { /*1, 10*/ };
            var result = user.Where(x => x.Id > 0 && x.Id < y && arr.Any(item => item == x.Id))
                .Select(x => new { x.Id, OldId = x.Id + 1, OOID = y });

            var list = result.ToList();

            /**
             * SELECT [x].[uid] AS [Id], ([x].[uid] + 1) AS [OldId], @y AS [OOID] 
             * FROM [fei_users] [x] 
             * WHERE ((([x].[uid] > 0) AND ([x].[uid] < @y)) AND [x].[uid] IN (1, 10))
             */
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
            /**
             * SELECT (CASE WHEN ([uid] > @__variable_1) THEN [uid] ELSE ([uid] + @__variable_2) END) AS [Id], ([uid] + @__variable_3) AS [OldId], (CASE WHEN (@y > @__variable_4) THEN @y ELSE @__variable_5 END) AS [OOID], @cc AS [DD] 
             * FROM [fei_users] 
             * WHERE (([uid] > @__variable_6) AND ([uid] < @y))
             */
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
            /**
             * SELECT (CASE WHEN ([uid] > @__variable_1) THEN [uid] ELSE ([uid] + @__variable_2) END) AS [Id], [username] AS [Name], @__variable_ticks AS [Time], ([uid] + @__variable_3) AS [OldId], [created_time] AS [Date] 
             * FROM [fei_users] 
             * WHERE ((([uid] < @__variable_4) AND [username] LIKE @str) AND ([created_time] < @__variable_now))
             */
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
            /**
             * SELECT [x].[uid] AS [Id], [x].[bcid], [x].[username], [x].[email], [x].[mobile], [x].[password], [x].[mallagid], [x].[salt], [x].[userstatus], [x].[created_time] AS [CreatedTime], [x].[modified_time] AS [ModifiedTime] 
             * FROM [fei_users] [x] 
             * WHERE ((([x].[uid] < @__variable_1) AND [x].[username] LIKE @str) AND ([x].[created_time] < @__variable_now)) 
             * ORDER BY [x].[created_time] DESC
             */
        }

        [TestMethod]
        public void WhereHasValueTest()
        {
            var str = "1";
            int? i = 1;
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 200 && x.Username.Contains(str) && (i.HasValue && x.Id > i.Value) && x.CreatedTime < DateTime.Now && x.Userstatus.HasValue);

            result = result.Where(x => x.Mallagid == 2);

            //if (i.HasValue)
            //{
            //result = result.Where(x => i.HasValue && x.Id > i.Value);
            //}
            var list = result.OrderBy(x => x.CreatedTime).ToList();
            /**
             * SELECT [x].[uid] AS [Id], [x].[bcid], [x].[username], [x].[email], [x].[mobile], [x].[password], [x].[mallagid], [x].[salt], [x].[userstatus], [x].[created_time] AS [CreatedTime], [x].[modified_time] AS [ModifiedTime] 
             * FROM [fei_users] [x] 
             * WHERE ([x].[uid] > @i) AND (((([x].[uid] < @__variable_1) AND [x].[username] LIKE @str) AND ([x].[created_time] < @__variable_now)) AND [x].[userstatus] IS NOT NULL) 
             * ORDER BY [x].[created_time] DESC
             */
        }
        [TestMethod]
        public void OrderBySelect()
        {
            var str = "1";
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 200 && x.Username.Contains(str) && x.CreatedTime < DateTime.Now);
            var list = result.OrderBy(x => x.CreatedTime).ToList();
            var count = result.Count();
            /**
             * SELECT [x].[uid] AS [Id], [x].[bcid], [x].[username], [x].[email], [x].[mobile], [x].[password], [x].[mallagid], [x].[salt], [x].[userstatus], [x].[created_time] AS [CreatedTime], [x].[modified_time] AS [ModifiedTime] 
             * FROM [fei_users] [x] 
             * WHERE ((([x].[uid] < @__variable_1) AND [x].[username] LIKE @str) AND ([x].[created_time] < @__variable_now)) ORDER BY [x].[created_time] DESC
             * 
             * SELECT Count(1) 
             * FROM [fei_users] [x] 
             * WHERE ((([x].[uid] < @__variable_1) AND [x].[username] LIKE @str) AND ([x].[created_time] < @__variable_now))
             */
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
            /**
             * SELECT [x].[uid] AS [Id], [x].[bcid], [x].[username], [x].[email], [x].[mobile], [x].[password], [x].[mallagid], [x].[salt], [x].[userstatus], [x].[created_time] AS [CreatedTime], [x].[modified_time] AS [ModifiedTime] FROM [fei_users] [x] 
             * WHERE ((([x].[uid] < @__variable_1) AND [x].[username] LIKE @str) AND ([x].[created_time] < @__variable_now)) 
             * ORDER BY [x].[created_time], [x].[bcid] DESC
             */
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
                .Reverse() //! 所有排序都逆转（不论前后）
                .OrderByDescending(x => x.Bcid);
            var list = result.ToList();
            /**
             * SELECT 
             * DISTINCT [x].[uid] AS [Id], [x].[bcid], [x].[username], [x].[email], [x].[mobile], [x].[password], [x].[mallagid], [x].[salt], [x].[userstatus], [x].[created_time] AS [CreatedTime], [x].[modified_time] AS [ModifiedTime] 
             * FROM [fei_users] [x] 
             * WHERE (([x].[uid] < @__variable_1) AND 
             * EXISTS(
             *      SELECT 
             *      DISTINCT [uid] AS [Id], [lastvisittime], [lastvisitip], [lastvisitrgid], [registertime], [registerip], [registerrgid], [gender], [bday], [idcard], [regionid], [address], [bio], [avatar], [realname], [nickname] 
             *      FROM [fei_userdetails] 
             *      WHERE (([uid] = [x].[uid]) AND ([uid] < @__variable_2)))
             * ) 
             * ORDER BY [x].[created_time] DESC, [x].[bcid]
             */
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

            /**
             * SELECT [x].[uid] AS [Id],
             * CASE WHEN [x].[username] IS NULL OR (LEN([x].[username]) - @__variable_2) < 1 
             *  THEN @__variable_3 
             *  ELSE SUBSTRING([x].[username],4,LEN([x].[username]) - @__variable_4) 
             * END AS [Name],@__variable_ticks AS [Time],([x].[uid]+@__variable_5) AS [OldId],[x].[created_time] AS [Date] 
             * FROM [fei_users] [x] 
             * WHERE (([x].[uid]<@__variable_1) AND ([x].[created_time]<@__variable_now))
             */
        }

        [TestMethod]
        public void ToUpperLowerTest()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now)
                .Select(x => new { x.Id, Name = x.Username.ToUpper(), Time = DateTime.Now.Ticks, OldId = x.Id + 1, Date = x.CreatedTime });
            var list = result.ToList();
            /**
             * SELECT [x].[uid] AS [Id], Upper([x].[username]) AS [Name], @ticks AS [Time], ([x].[uid] + 1) AS [OldId], [x].[created_time] AS [Date] 
             * FROM [fei_users] [x] 
             * WHERE (([x].[uid] < 100) AND ([x].[created_time] < @now))
             */
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
            /**
             * SELECT [x].[uid] AS [Id], [x].[username] AS [Name], @__variable_ticks AS [Time], ([x].[uid] + 1) AS [OldId], [x].[created_time] AS [Date] 
             * FROM [fei_users] [x] 
             * WHERE ((([x].[uid] < 100) AND ([x].[created_time] < @__variable_now)) AND ([x].[username] IS  NOT NULL AND [x].[username]<>''))
             */
        }
        [TestMethod]
        public void IndexOf()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now && x.Username.IndexOf("in") > 1)
                .Select(x => new { x.Id, Name = x.Username, Time = DateTime.Now.Ticks, OldId = x.Id + 1, Date = x.CreatedTime });
            var list = result.ToList();
            /**
             * SELECT [x].[uid] AS [Id],[x].[username] AS [Name],@__variable_ticks AS [Time],([x].[uid]+@__variable_5) AS [OldId],[x].[created_time] AS [Date] 
             * FROM [fei_users] [x] 
             * WHERE ((([x].[uid]<@__variable_1) AND ([x].[created_time]<@__variable_now)) 
             * AND ((CASE WHEN @__variable_3 IS NULL OR [x].[username] IS NULL THEN -1 ELSE CHARINDEX(@__variable_4,[x].[username]) - 1 END)>@__variable_2))
             */
        }
        [TestMethod]
        public void MaxTest()
        {
            var user = new UserRepository();
            var result = user.Max();
            /**
             * SELECT Max([uid]) AS [Id], Max([bcid]) AS [Bcid], Max([username]) AS [Username], Max([email]) AS [Email], Max([mobile]) AS [Mobile], Max([password]) AS [Password], Max([mallagid]) AS [Mallagid], Max([salt]) AS [Salt], Max([userstatus]) AS [Userstatus], Max([created_time]) AS [CreatedTime], Max([modified_time]) AS [ModifiedTime], Max([actionlist]) AS [Actionlist] 
             * FROM [fei_users]
             */
        }
        [TestMethod]
        public void MaxWithTest()
        {
            var user = new UserRepository();
            var result = user.Max(x => x.Id);
            /**
             * SELECT Max([uid]) FROM [fei_users]
             */
        }

        [TestMethod]
        public void WhereMaxTest()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now).Max();
            /**
             * SELECT Max([x].[uid]) AS [Id], Max([x].[bcid]) AS [Bcid], Max([x].[username]) AS [Username], Max([x].[email]) AS [Email], Max([x].[mobile]) AS [Mobile], Max([x].[password]) AS [Password], Max([x].[mallagid]) AS [Mallagid], Max([x].[salt]) AS [Salt], Max([x].[userstatus]) AS [Userstatus], Max([x].[created_time]) AS [CreatedTime], Max([x].[modified_time]) AS [ModifiedTime] 
             * FROM [fei_users] [x] 
             * WHERE (([x].[uid] < 100) AND ([x].[created_time] < @now))
             */
        }

        [TestMethod]
        public void DistinctTest()
        {
            var user = new UserRepository();
            var result = user.Distinct();
            var list = result.ToList();
            /**
             * SELECT 
             * DISTINCT [uid] AS [Id], [bcid], [username], [email], [mobile], [password], [mallagid], [salt], [userstatus], [created_time] AS [CreatedTime], [modified_time] AS [ModifiedTime], [actionlist] 
             * FROM [fei_users]
             */
        }

        [TestMethod]
        public void DistinctSelectTest()
        {
            var user = new UserRepository();
            //var result = user.Distinct().Select(x => x.Id);
            //var list = result.ToList();
            var result = user.Distinct().Select(x => x.Id);
            var list = result.ToList();
            /**
             * SELECT 
             * DISTINCT [x].[uid] 
             * FROM [fei_users] [x]
             */
        }

        [TestMethod]
        public void DistinctWhereTest()
        {
            var user = new UserRepository();
            //var result = user.Distinct().Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now);
            //var list = result.ToList();
            var result = user.Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now).Distinct();
            var list = result.ToList();
            /**
             * SELECT 
             * DISTINCT [x].[uid] AS [Id], [x].[bcid], [x].[username], [x].[email], [x].[mobile], [x].[password], [x].[mallagid], [x].[salt], [x].[userstatus], [x].[created_time] AS [CreatedTime], [x].[modified_time] AS [ModifiedTime] 
             * FROM [fei_users] [x] 
             * WHERE (([x].[uid] < @__variable_1) AND ([x].[created_time] < @__variable_now))
             */
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

            /**
             * SELECT [x].[uid] AS [Id], ([x].[uid] + @__variable_1) AS [OldId], [d].[uid] AS [OOID], @y AS [DDD] 
             * FROM [fei_users] [x] 
             * LEFT JOIN [fei_userdetails] [d] 
             * ON [x].[uid]=[d].[uid] 
             * WHERE ((([x].[uid] > @__variable_2) AND ([d].[uid] < @y)) AND [x].[username] LIKE @str) ORDER BY [x].[uid] DESC, [d].[registertime]
             */
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

            /**
             * SELECT [x].[uid] AS [Id],
             * ([x].[uid]+@__variable_2) AS [OldId],
             * [z].[openid] AS [Openid] 
             * FROM [fei_users] [x] 
             * LEFT JOIN [fei_userdetails] [y] ON [x].[uid]=[y].[uid] 
             * LEFT JOIN [fei_user_wx_account_info] [z] ON [x].[uid]=[z].[uid] 
             * WHERE ((([x].[uid]>@__variable_1) AND ([y].[uid]<@y1)) AND [x].[username] LIKE @str) 
             * ORDER BY [x].[uid],[y].[registertime] DESC
             */
        }

        [TestMethod]
        public void JoinTest3()
        {
            var y = 100;
            var str = "1";
            var user = new UserRepository();
            var details = new UserDetailsRepository();
            var userWx = new UserWeChatRepository();
            var result = from x in user
                         join d in details on x.Id equals d.Id
                         join w in userWx on x.Id equals w.Uid
                         where x.Id > 0 && d.Id < y && x.Username.Contains(str)
                         orderby x.Id, d.Registertime descending
                         select new { x.Id, OldId = x.Id + 1, w.Openid, OOID = d.Id, DDD = y };

            var list = result.Count();

            /**
             * SELECT [x].[uid] AS [Id], ([x].[uid] + @__variable_1) AS [OldId], [d].[uid] AS [OOID], @y AS [DDD] 
             * FROM [fei_users] [x] 
             * LEFT JOIN [fei_userdetails] [d] 
             * ON [x].[uid]=[d].[uid] 
             * WHERE ((([x].[uid] > @__variable_2) AND ([d].[uid] < @y)) AND [x].[username] LIKE @str) ORDER BY [x].[uid] DESC, [d].[registertime]
             */
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

            /**
             * SELECT Count(1) 
             * FROM [fei_users] [x] 
             * LEFT JOIN [fei_userdetails] [y] 
             * ON [x].[uid]=[y].[uid] 
             * WHERE ((([x].[uid] > @__variable_1) AND ([y].[uid] < @id)) AND [x].[username] LIKE @str)
             */
        }


        [TestMethod]
        public void TakeTest()
        {
            var user = new UserRepository();
            //var result = user.Distinct().Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now);
            //var list = result.ToList();
            var result = user.From(x => x.TableName).Take(10);
            var list = result.ToList();
            /**
             *  SELECT TOP 10 [uid] AS [Id], [bcid], [username], [email], [mobile], [password], [mallagid], [salt], [userstatus], [created_time] AS [CreatedTime], [modified_time] AS [ModifiedTime] 
             *  FROM [fei_users]
             */
        }
        [TestMethod]
        public void TakeWhereTest()
        {
            var user = new UserRepository();
            //var result = user.Distinct().Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now);
            //var list = result.ToList();
            var result = user.Take(10).Where(x => x.Id < 100 && x.CreatedTime < DateTime.Now);
            var list = result.ToList();
            /**
             *  SELECT TOP 10 [uid] AS [Id], [bcid], [username], [email], [mobile], [password], [mallagid], [salt], [userstatus], [created_time] AS [CreatedTime], [modified_time] AS [ModifiedTime] 
             *  FROM [fei_users] 
             *  WHERE (([uid] < 100) AND ([created_time] < @__variable_now))
             */
        }

        [TestMethod]
        public void SkipOrderByTest() //! SqlServer中，必须配合排序函数（OrderBy/OrderByDescending）使用
        {
            var user = new UserRepository();
            var result = user.Skip(124680).OrderBy(x => x.Id);
            var list = result.ToList();
            /**
             * SELECT * FROM (
             *  SELECT ROW_NUMBER() OVER(ORDER BY [uid] DESC) AS [__Row_number_],[uid] AS [Id], [bcid], [username], [email], [mobile], [password], [mallagid], [salt], [userstatus], [created_time] AS [CreatedTime], [modified_time] AS [ModifiedTime] 
             *  FROM [fei_users] 
             * ) [CTE] 
             * WHERE [__Row_number_] > 124680
             */
        }
        [TestMethod]
        public void TakeSkipOrderByTest()
        {
            var user = new UserRepository();
            var result = user.Take(10).Skip(10000).OrderBy(x => x.Id);
            var list = result.ToList();
            /**
             * SELECT [Id],[bcid],[username],[email],[mobile],[password],[mallagid],[salt],[userstatus],[CreatedTime],[ModifiedTime] FROM 
             * (
             *   SELECT ROW_NUMBER() OVER(ORDER BY [uid]) AS [__Row_number_],[uid] AS [Id], [bcid], [username], [email], [mobile], [password], [mallagid], [salt], [userstatus], [created_time] AS [CreatedTime], [modified_time] AS [ModifiedTime] 
             *   FROM [fei_users] 
             * ) [CTE]
             * WHERE [__Row_number_] > 10000 AND [__Row_number_] <= 10010
             */
        }
        [TestMethod]
        public void FirstOrDefaultTest()
        {

            var user = new UserRepository();
            var userEntity = user.FirstOrDefault();
            /**
             *  SELECT TOP 1 [uid] AS [Id], [bcid], [username], [email], [mobile], [password], [mallagid], [salt], [userstatus], [created_time] AS [CreatedTime], [modified_time] AS [ModifiedTime] 
             *  FROM [fei_users]
             */
        }
        [TestMethod]
        public void FirstOrDefaultWhereTest()
        {
            var user = new UserRepository();

            //string sql = user.Where(x => x.Username.Length > 10 && x.Id < 100 && x.CreatedTime < DateTime.Now)
            //    .OrderBy(x => x.Id).Sql();

            var userEntity = user.Where(x => x.Username.Length > 10 && x.Id < 100 && x.CreatedTime < DateTime.Now)
                .OrderBy(x => x.Id)
                .FirstOrDefault();
            /**
             *   SELECT TOP 1 [x].[uid] AS [Id], [x].[bcid], [x].[username], [x].[email], [x].[mobile], [x].[password], [x].[mallagid], [x].[salt], [x].[userstatus], [x].[created_time] AS [CreatedTime], [x].[modified_time] AS [ModifiedTime] 
             *   FROM [fei_users] [x] 
             *   WHERE (([x].[uid] < 100) AND ([x].[created_time] < @__variable_now))
             */
        }

#if NETSTANDARD2_1

        [TestMethod]
        public void TakeLastOrderByTest() //! 必须配合排序函数（OrderBy/OrderByDescending）使用
        {
            var user = new UserRepository();

            var results = user.TakeLast(10).OrderBy(x => x.CreatedTime).ToList();

            /**
             *  SELECT 
             *  TOP 10 [uid] AS [Id], [bcid], [username], [email], [mobile], [password], [mallagid], [salt], [userstatus], [created_time] AS [CreatedTime], [modified_time] AS [ModifiedTime] 
             *  FROM [fei_users] 
             *  ORDER BY [created_time] DESC
             */
        }


        [TestMethod]
        public void SkipLastOrderByTest()  //! 必须配合排序函数（OrderBy/OrderByDescending）使用
        {
            var user = new UserRepository();

            var results = user.OrderBy(x => x.CreatedTime).SkipLast(124680).ToList();

            /**
             *   SELECT [Id],[bcid],[username],[email],[mobile],[password],[mallagid],[salt],[userstatus],[CreatedTime],[ModifiedTime] 
             *   FROM (
             *      SELECT ROW_NUMBER() OVER(ORDER BY [created_time] DESC) AS [__Row_number_],[uid] AS [Id], [bcid], [username], [email], [mobile], [password], [mallagid], [salt], [userstatus], [created_time] AS [CreatedTime], [modified_time] AS [ModifiedTime] 
             *      FROM [fei_users] 
             *   ) [CTE] 
             *   WHERE [__Row_number_] > 124680
             */
        }
#endif
        [TestMethod]
        public void TakeWhileTest()
        {
            var str = "1";
            var user = new UserRepository();
            var results = user.TakeWhile(x => x.Id < 200 && x.Username.Contains(str) && x.CreatedTime < DateTime.Now).Take(10).ToList();
            /**
             *  SELECT TOP 10 [x].[uid] AS [Id], [x].[bcid], [x].[username], [x].[email], [x].[mobile], [x].[password], [x].[mallagid], [x].[salt], [x].[userstatus], [x].[created_time] AS [CreatedTime], [x].[modified_time] AS [ModifiedTime] 
             *  FROM [fei_users] [x] 
             *  WHERE ((([x].[uid] < @__variable_1) AND [x].[username] LIKE @str) AND ([x].[created_time] < @__variable_now))
             */
        }

        [TestMethod]
        public void SkipWhileTest()
        {
            var str = "1";
            var user = new UserRepository();
            var results = user.SkipWhile(x => x.Id < 200 && x.Username.Contains(str) && x.CreatedTime < DateTime.Now).Take(10).ToList();

            /**
             *  SELECT 
             *  TOP 10 [x].[uid] AS [Id], [x].[bcid], [x].[username], [x].[email], [x].[mobile], [x].[password], [x].[mallagid], [x].[salt], [x].[userstatus], [x].[created_time] AS [CreatedTime], [x].[modified_time] AS [ModifiedTime] 
             *  FROM [fei_users] [x] 
             *  WHERE ((([x].[uid] >= @__variable_1) OR [x].[username] NOT LIKE @str) OR ([x].[created_time] >= @__variable_now))
             *  
             * -- @__variable_1 200
             * -- @str "1"
             * -- @__variable_now DateTime.Now
             */
        }
        [TestMethod]
        public void CastTest() //? 在SQL中，只会生成共有的属性(不区分大小写)。
        {
            var y = 100;
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y).Cast<UserSimDto>();

            var list = result.ToList();

            /**
             * SELECT [x].[uid] AS [Id], [x].[username] 
             * FROM [fei_users] [x] 
             * WHERE (([x].[uid] > @__variable_1) AND ([x].[uid] < @y))
             */
        }
        [TestMethod]
        public void CastCountTest() //? 在SQL中，只会生成共有的属性(不区分大小写)。
        {
            var y = 100;
            var user = new UserRepository();
            var result = user.Where(x => x.Id > 0 && x.Id < y).Cast<UserSimDto>();

            var list = result.Count();

            /**
             * SELECT [x].[uid] AS [Id], [x].[username] 
             * FROM [fei_users] [x] 
             * WHERE (([x].[uid] > @__variable_1) AND ([x].[uid] < @y))
             */
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
            /**
             * SELECT TOP 10 * 
             * FROM (
             *  SELECT [x].[uid] AS [Id], [x].[username] AS [Username] 
             *  FROM [fei_users] [x] 
             *  WHERE ((([x].[uid] > @__variable_1) AND ([x].[uid] < @y)) AND [x].[username] LIKE @__variable_2)
             *  
             *  UNION 
             *  
             *  SELECT [x1].[uid] AS [Id], [x1].[realname] AS [Username] 
             *  FROM [fei_userdetails] [x1] 
             *  WHERE (([x1].[uid] > @__variable_3) AND ([x1].[uid] < @y))
             * ) [CTE]
             */
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
            /**
             * SELECT Count(1) FROM (
             *  SELECT [x].[uid] AS [Id],[x].[username] AS [Username] 
             *  FROM [fei_users] [x] 
             *  WHERE ((([x].[uid]>@__variable_1) AND ([x].[uid]<@y)) AND [x].[username] LIKE @__variable_2) 
             *  
             *  UNION 
             *  
             *  SELECT [x].[uid] AS [Id],[x].[realname] AS [Username] 
             *  FROM [fei_userdetails] [x] 
             *  WHERE (([x].[uid]>@__variable_3) AND ([x].[uid]<@y))
             * ) [CTE_UNION]
             */
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

            /**
             * SELECT TOP 10 * FROM (
             *  SELECT [x].[uid] AS [Id],[x].[username] AS [Username] 
             *  FROM [fei_users] [x] WHERE ((([x].[uid]>@__variable_1) AND ([x].[uid]<@y)) AND [x].[username] LIKE @__variable_2) 
             *  
             *  INTERSECT 
             *  
             *  SELECT [x].[uid] AS [Id],[x].[realname] AS [Username] 
             *  FROM [fei_userdetails] [x] WHERE (([x].[uid]>@__variable_3) AND ([x].[uid]<@y))
             * ) [CTE]
             */
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

            /**
             * SELECT Count(1) FROM (
             *  SELECT [x].[uid] AS [Id],[x].[username] AS [Username] 
             *  FROM [fei_users] [x] 
             *  WHERE ((([x].[uid]>@__variable_1) AND ([x].[uid]<@y)) AND [x].[username] LIKE @__variable_2) 
             *  
             *  INTERSECT 
             *  
             *  SELECT [x].[uid] AS [Id],[x].[realname] AS [Username] 
             *  FROM [fei_userdetails] [x] 
             *  WHERE (([x].[uid]>@__variable_3) AND ([x].[uid]<@y))
             * ) [CTE_UNION]
             */
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

            /**
             * SELECT [x].[uid] AS [Id] 
             * FROM [fei_users] [x] 
             * WHERE (([x].[uid] > @__variable_1) AND ([x].[uid] < @y)) -- CreatedTime 已被被忽略。
             */
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

            /**
             * SELECT Count(1) 
             * FROM [fei_users] 
             * WHERE ([mallagid]=@__variable_1)
             */
        }

        [TestMethod]
        public void CustomFirstWithMethodTest()
        {
            var user = new UserRepository();
            var result = user.Where(x => x.Id > user.Skip(100000).OrderBy(y => y.CreatedTime).TakeFirst(y => y.Id) && x.CreatedTime < DateTime.Now)
                .OrderBy(x => x.CreatedTime)
                .Take(10)
                .Skip(100)
                .Select(x => x.Username.Substring(2))
                .ToList();

            /**
             * SELECT [__sql_server_col] 
             * FROM (
             *  SELECT SUBSTRING([x].[username],@__variable_1 + 1,LEN([x].[username]) - @__variable_2) AS [__sql_server_col], 
             *  ROW_NUMBER() OVER(ORDER BY [x].[created_time]) AS [__Row_number_] 
             *  FROM [fei_users] [x]
             *  WHERE (([x].[uid]>(
             *      SELECT [uid] FROM (
             *          SELECT [y].[uid], 
             *          ROW_NUMBER() OVER(ORDER BY [y].[created_time]) AS [__Row_number_] 
             *          FROM [fei_users] [y] ) [CTE] 
             *          WHERE [__Row_number_] > 100000 AND [__Row_number_]<=100001)
             *      ) 
             *  AND ([x].[created_time]<@__variable_now)) 
             * ) [CTE] 
             * WHERE [__Row_number_] > 100 AND [__Row_number_]<=110
             */
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

            /**
             *  SELECT TOP 1 [x].[uid] AS [Id],[x].[bcid],[x].[username],[x].[email],[x].[mobile],[x].[password],[x].[mallagid],[x].[salt],[x].[userstatus],[x].[created_time] AS [CreatedTime],[x].[modified_time] AS [ModifiedTime] 
             *  FROM [fei_users] [x] 
             *  WHERE ([x].[uid]<@__variable_1)
             */
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

            /**
             *  SELECT TOP 1 [x].[uid] AS [Id],[x].[bcid],[x].[username],[x].[email],[x].[mobile],[x].[password],[x].[mallagid],[x].[salt],[x].[userstatus],[x].[created_time] AS [CreatedTime],[x].[modified_time] AS [ModifiedTime] 
             *  FROM [fei_users] [x] 
             *  WHERE ([x].[uid]<@__variable_1)
             */
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
                //Username = "admin",
                Userstatus = 1,
                Mobile = "18980861011",
                Email = "tinylit@foxmail.com",
                Password = "123456",
                Salt = string.Empty,
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };

            var i = user.AsUpdateable(entry)
                .Limit(x => x.Password)
                .Where(x => x.Username ?? x.Mobile)
                .ExecuteCommand();

            var j = user.AsExecuteable()
                .From(x => x.TableName)
                .Where(x => x.Username == "admin")
                .Update(x => new FeiUsers
                {
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

            var list = new List<FeiUsers>();

            for (int i = 0; i < 1000; i++)
            {
                list.Add(entry);
            }

            var x1 = user.AsDeleteable(list)
                .Where(x => x.Username)
                .ExecuteCommand();

            var x2 = user.AsExecuteable()
                .Delete(x => x.Username == "admi");
        }

        [TestMethod]
        public void ApplyTest()
        {
            var user = new UserRepository();

            var dto = user.GetApply();
        }
    }
}

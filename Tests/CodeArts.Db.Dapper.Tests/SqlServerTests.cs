using CodeArts.Casting;
using CodeArts.Db.Tests;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Text;
using UnitTest.Domain.Entities;

namespace CodeArts.Db.Dapper.Tests
{
    public class Tests
    {
        private static bool isCompleted;

        private static readonly DbContext context;

        static Tests() => context = new DbTestContext();

        [SetUp]
        public void Setup()
        {
            var adapter = new SqlServerAdapter();
            DapperConnectionManager.RegisterAdapter(adapter);

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

        [Test]
        public void QueryFirst()
        {
            string sql = "SELECT * FROM fei_userdetails WHERE uid>@uid";

            var entry = context.QueryFirstOrDefault<FeiUserdetails>(sql, new { uid = 100 });
        }

        [Test]
        public void Query()
        {
            string sql = "SELECT * FROM fei_users WHERE uid>@uid";

            var results = context.Query<FeiUsers>(sql, new { uid = 1 });
        }

        [Test]
        public void QueryPaged()
        {
            string sql = "SELECT * FROM fei_users WHERE uid>@uid ORDER BY uid";

            var results = context.Query<FeiUsers>(sql, 0, 10, new { uid = 1 });
        }

        [Test]
        public void QueryNestedSelect()
        {
            string sql = "SELECT (SELECT uid FROM fei_userdetails WHERE uid=x.uid), * FROM fei_users x WHERE uid>@uid ORDER BY uid,(SELECT uid FROM fei_userdetails WHERE uid=x.uid)";

            var results = context.Query<FeiUsers>(sql, 0, 10, new { uid = 1 });
        }
    }
}
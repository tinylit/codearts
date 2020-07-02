using System;
using CodeArts.DbAnnotations;
using CodeArts.SqlServer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using UnitTest.Domain.Entities;
using UnitTest.Serialize;
using System.Linq;
using System.Collections.Concurrent;
using System.Timers;
using System.Data;
using CodeArts.ORM.Exceptions;

namespace CodeArts.ORM.Tests
{
    [SqlServerConnection]
    [TypeGen(typeof(DbTypeGen))]
    public interface IUser : IDbMapper<FeiUsers>
    {
        [Select("SELECT * FROM fei_users WHERE uid>={id} AND uid<{max_id}")]
        List<FeiUsers> GetUsers(int id, int max_id);

        [Select("SELECT * FROM fei_users WHERE uid>={id} AND uid<{max_id}")]
        List<T> GetUsers<T>(int id, int max_id);

        [Select("SELECT * FROM fei_users WHERE uid={id}")]
        FeiUsers GetUser(int id);

        [Select("SELECT * FROM fei_users WHERE uid={id}")]
        T GetUser<T>(int id);

        [Select("SELECT * FROM fei_users WHERE uid={id}", true)]
        FeiUsers GetUserRequired(int id);
    }

    [TestClass]
    public class EmitTest
    {
        [TestMethod]
        public void MyTestMethod()
        {
            var adapter = new SqlServerAdapter();
            DbConnectionManager.RegisterAdapter(adapter);
            DbConnectionManager.RegisterProvider<CodeArtsProvider>();

            IUser user = (IUser)System.Activator.CreateInstance(new DbTypeGen().Create(typeof(IUser)));

            var users = user.GetUsers(10, 100);

            var user2s = user.GetUsers<FeiUsers>(10, 100);

            Assert.IsFalse(users is null);

            var userDto = user.GetUser(91);

            var useDto3 = user.GetUser<FeiUsers>(90);

            try
            {
                var userDto2 = user.GetUserRequired(91);
            }
            catch (DRequiredException)
            {
            }

        }
    }
}

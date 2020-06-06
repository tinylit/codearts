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

namespace CodeArts.ORM.Tests
{
    [SqlServerConnection]
    [TypeGen(typeof(DbTypeGen))]
    public interface IUser : IDbMapper<FeiUsers>
    {
        [Select("SELECT * FROM fei_users WHERE uid>={id} AND uid<{max_id}")]
        List<FeiUsers> GetUser(int id, int max_id);

        [Select("SELECT * FROM fei_users WHERE uid={id}")]
        FeiUsers GetUser(int id);
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

            var userDto = user.GetUser(10, 100);

            Assert.IsFalse(userDto is null);
        }
    }
}

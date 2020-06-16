using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CodeArts.Tests
{
    [TestClass]
    public class CryptoExtentions
    {
        [TestMethod]
        public void MyTestMethod()
        {
            var value = "d;ljvhhgortpghfgbfgbgbg";

            var encrypt = value.Encrypt("@%d^#41&", "%@D^d$2~");

            var decrypt = encrypt.Decrypt("@%d^#41&", "%@D^d$2~");

            Assert.IsTrue(value == decrypt);
        }

        [TestMethod]
        public void MyTestMethod2()
        {
            var value = "d;ljvhhgortpghfgbfgbgbg";

            var encrypt = value.Encrypt("92N0La5}AC$@efgt", CryptoKind.AES);

            var decrypt = encrypt.Decrypt("92N0La5}AC$@efgt", CryptoKind.AES);

            Assert.IsTrue(value == decrypt);
        }
    }
}

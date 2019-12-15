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
        }
    }
}

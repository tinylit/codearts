using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeArts.Tests
{
    [TestClass]
    public class StringExtentions
    {
        [TestMethod]
        public void PropSugar()
        {
            for (int i = 0; i < 100000; i++)
            {
                string value = $"{i}xxx{{x ?? z}}-{{y?+z}}-{{z}}--{{xyz+sb}}-{{sb}}-{{abc}}".PropSugar(new { x = 1, y = DateTime.Now, z = "测试", xyz = new int[] { 1, 2, 3 }, sb = new StringBuilder("sb") }, new JsonSettings(NamingType.Normal));
            }
        }
    }
}

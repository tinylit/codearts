using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeArts.Tests
{
    /// <summary>
    /// 常规测试。
    /// </summary>
    [TestClass]
    public class NormalTests
    {
        private int usingIndex = 0;

        private readonly StringBuilder sb = new StringBuilder();

        private readonly Stack<int> usingList = new Stack<int>();

        private readonly Dictionary<int, int> usingCache = new Dictionary<int, int>();

        /// <summary>
        /// 动作。
        /// </summary>
        /// <param name="usingAction">动作。</param>
        /// <returns></returns>
        private string UsingAction(Action usingAction)
        {
            int startIndex = sb.Length;

            if (usingIndex > 0)
            {
                usingCache[usingIndex] = startIndex;

                usingList.Push(startIndex);
            }

            usingIndex++;

            usingAction.Invoke();

            if (usingList.Count == usingIndex)
            {
                usingIndex--;

                return sb.ToString(startIndex, usingList.Pop() - startIndex);
            }
            //else if (usingCache.TryGetValue(usingIndex, out int cacheIndex))
            //{
            //    usingIndex--;

            //    return sb.ToString(startIndex, cacheIndex - startIndex);
            //}

            usingIndex--;

            return sb.ToString(startIndex, sb.Length - startIndex);
        }

        [TestMethod]
        public void Recursion()
        {
            int testDepth = 0;
            string value = UsingAction(() =>
              {
                  sb.Append("TEST")
                  .Append(++testDepth);

                  string value1 = UsingAction(() =>
                  {
                      sb.Append("TEST")
                      .Append(++testDepth);

                      string value2 = UsingAction(() =>
                      {
                          sb.Append("TEST")
                          .Append(++testDepth);
                      });
                  });
              });
        }
    }
}

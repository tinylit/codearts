using System;
using System.Collections.Generic;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 配置
    /// </summary>
    public static class CommonSettings
    {
        /// <summary>
        /// 参数分析
        /// </summary>
        /// <param name="value">执行语句</param>
        /// <param name="startIndex">参数起始位置</param>
        /// <param name="leftCount">左括号出现次数</param>
        /// <param name="rightCount">右括号出现次数</param>
        /// <returns></returns>
        private static int ParameterAnalysis(string value, int startIndex, int leftCount, int rightCount)
        {
            int index = Array.FindIndex(value.ToCharArray(startIndex, value.Length - startIndex), item =>
            {
                if (item == ')')
                {
                    rightCount += 1;

                    return rightCount == leftCount;
                }

                if (item == '(')
                {
                    leftCount += 1;
                }

                return false;
            });

            return index > -1 ? index + startIndex + 1 : index;
        }

        /// <summary>
        /// 参数分析
        /// </summary>
        /// <param name="value">查询语句</param>
        /// <param name="startIndex">参数起始位置</param>
        /// <returns></returns>
        private static int ParameterAnalysis(string value, int startIndex)
        {
            var indexOf = value.IndexOf("CASE", startIndex, StringComparison.OrdinalIgnoreCase);

            if (indexOf > -1 && (startIndex == indexOf || string.IsNullOrWhiteSpace(value.Substring(startIndex, indexOf - startIndex))))
            {
                startIndex = indexOf;

                int indexCase, indexEnd;

                do
                {
                    indexEnd = value.IndexOf("END", startIndex + 4, StringComparison.OrdinalIgnoreCase);

                    if (indexEnd == value.Length)
                    {
                        return indexEnd;
                    }

                    indexCase = value.IndexOf("CASE", startIndex + 4, StringComparison.OrdinalIgnoreCase);

                    startIndex = indexEnd;

                } while (indexCase > -1 && indexEnd > indexCase);

                if (startIndex == value.Length)
                {
                    return startIndex;
                }
            }

            int index = value.IndexOf(',', startIndex);

            //? 不包含分割符
            if (index == -1) return index;

            int leftIndex = value.IndexOf('(', startIndex, index - startIndex);

            //? 不包含左括号
            if (leftIndex == -1) return index;

            return ParameterAnalysis(value, leftIndex + 1, 1, 0);
        }

        /// <summary>
        /// 每列代码块（如:[x].[id],substring([x].[value],[x].[index],[x].[len]) as [total] => new List<string>{ "[x].[id]","substring([x].[value],[x].[index],[x].[len]) as [total]" }）
        /// </summary>
        /// <param name="columns">以“,”分割的列集合</param>
        /// <returns></returns>
        public static List<string> ToSingleColumnCodeBlock(string columns)
        {
            int startIndex = 0, nextIndex = 0, length = columns.Length;
            List<string> list = new List<string>();
            while (nextIndex > -1 && startIndex < length)
            {
                nextIndex = ParameterAnalysis(columns, startIndex);

                if (nextIndex == -1)
                {
                    list.Add(columns.Substring(startIndex));
                }
                else
                {
                    list.Add(columns.Substring(startIndex, nextIndex - startIndex));
                }

                startIndex = nextIndex + 1;
            }

            return list;
        }
    }
}

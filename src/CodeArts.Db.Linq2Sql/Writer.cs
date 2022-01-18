using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace CodeArts.Db
{
    /// <summary>
    /// SQL写入流。
    /// </summary>
    public class Writer
    {
        private readonly IWriterMap writerMap;

        private readonly StringBuilder sb;

        private readonly StringBuilder sbOrder;

        private readonly ISQLCorrectSettings settings;

        private readonly Writer writer;
        private bool writeOrderBy = false;

        private int parameterIndex = 0;

        /// <summary>
        /// 参数名称。
        /// </summary>
        protected virtual string ParameterName => string.Concat("__variable_", ParameterIndex.ToString());

        /// <summary>
        /// 参数索引。
        /// </summary>
        protected virtual int ParameterIndex => writer is null ? ++parameterIndex : writer.ParameterIndex;

        private int appendAt = -1;

        private int appendOrderByAt = -1;

        /// <summary>
        /// 写入位置。
        /// </summary>
        public int AppendAt
        {
            get => writeOrderBy ? appendOrderByAt : appendAt;
            set
            {
                if (writeOrderBy)
                {
                    appendOrderByAt = value;
                }
                else
                {
                    appendAt = value;
                }
            }
        }

        /// <summary>
        /// 内容长度。
        /// </summary>
        public int Length => writeOrderBy ? sbOrder.Length : sb.Length;

        /// <summary>
        /// 条件取反。
        /// </summary>
        public bool IsReverseCondition { get; private set; }

        /// <summary>
        /// 移除数据。
        /// </summary>
        /// <param name="index">索引开始位置。</param>
        /// <param name="lenght">移除字符长度。</param>
        public void Remove(int index, int lenght)
        {
            if (writeOrderBy)
            {
                sbOrder.Remove(index, lenght);
            }
            else
            {
                sb.Remove(index, lenght);
            }
        }

        private Dictionary<string, object> parameters;
        /// <summary>
        /// 参数集合。
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER
        public Dictionary<string, object> Parameters => parameters ??= new Dictionary<string, object>();
#else
        public Dictionary<string, object> Parameters => parameters ?? (parameters = new Dictionary<string, object>());
#endif

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="settings">SQL矫正配置。</param>
        /// <param name="writer">写入配置。</param>
        /// <param name="parameters">参数。</param>
        public Writer(ISQLCorrectSettings settings, IWriterMap writer, Dictionary<string, object> parameters)
        {
            sb = new StringBuilder();

            sbOrder = new StringBuilder();

            this.parameters = parameters ?? new Dictionary<string, object>();

            this.writerMap = writer ?? throw new ArgumentNullException(nameof(writer));

            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="writer">写入器。</param>
        public Writer(Writer writer)
        {
            sb = new StringBuilder();

            sbOrder = new StringBuilder();

            this.writer = writer;

            this.parameters = writer.parameters;

            this.writerMap = writer.writerMap;

            this.settings = writer.settings;
        }

        /// <summary>
        /// )
        /// </summary>
        public void CloseBrace() => Write(writerMap.CloseBrace);

        /// <summary>
        /// ,
        /// </summary>
        public void Delimiter() => Write(writerMap.Delimiter);

        /// <summary>
        /// DISTINCT
        /// </summary>
        public virtual void Distinct()
        {
            Write("DISTINCT");
            WhiteSpace();
        }

        /// <summary>
        /// ''
        /// </summary>
        public void EmptyString() => Write(writerMap.EmptyString);

        /// <summary>
        /// Exists
        /// </summary>
        public void Exists()
        {
            if (IsReverseCondition)
            {
                Not();
            }

            Write("EXISTS");
        }

        /// <summary>
        /// Like
        /// </summary>
        public void Like()
        {
            WhiteSpace();

            if (IsReverseCondition)
            {
                Not();
            }

            Write("LIKE");

            WhiteSpace();
        }

        /// <summary>
        /// IN
        /// </summary>
        public void Contains()
        {
            WhiteSpace();

            if (IsReverseCondition)
            {
                Not();
            }

            Write("IN");
        }

        /// <summary>
        /// From
        /// </summary>
        public void From()
        {
            WhiteSpace();
            Write("FROM");
            WhiteSpace();
        }

        /// <summary>
        /// Inner Join
        /// </summary>
        public void Join()
        {
            WhiteSpace();
            Write("INNER");
            WhiteSpace();
            Write("JOIN");
            WhiteSpace();
        }

        /// <summary>
        /// Left Join
        /// </summary>
        public void GroupJoin()
        {
            WhiteSpace();
            Write("LEFT");
            WhiteSpace();
            Write("JOIN");
            WhiteSpace();
        }

        /// <summary>
        /// (
        /// </summary>
        public void OpenBrace() => Write(writerMap.OpenBrace);

        /// <summary>
        /// Group By
        /// </summary>
        public void GroupBy()
        {
            WhiteSpace();
            Write("GROUP");
            WhiteSpace();
            Write("BY");
            WhiteSpace();
        }

        /// <summary>
        /// Order By
        /// </summary>
        public void OrderBy()
        {
            WhiteSpace();
            Write("ORDER");
            WhiteSpace();
            Write("BY");
            WhiteSpace();
        }

        /// <summary>
        /// 参数。
        /// </summary>
        /// <param name="parameterValue">参数值。</param>
        public virtual void Parameter(object parameterValue)
        {
            if (parameterValue is null)
            {
                Null();

                return;
            }

            var type = parameterValue.GetType();

            if (!type.IsValueType)
            {
                Parameter(ParameterName, parameterValue);

                return;
            }

            var cType = type;

            if (cType.IsNullable())
            {
                cType = Nullable.GetUnderlyingType(type);
            }

            if (cType.IsEnum)
            {
                cType = Enum.GetUnderlyingType(cType);
            }

            switch (Type.GetTypeCode(cType))
            {
                case TypeCode.Boolean:
                    if (Equals(parameterValue, true))
                    {
                        BooleanTrue();
                    }
                    else
                    {
                        BooleanFalse();
                    }
                    break;
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:

                    if (type.IsEnum || cType.IsEnum)
                    {
                        parameterValue = Convert.ChangeType(parameterValue, cType);
                    }

                    Write(parameterValue.ToString());

                    break;
                default:
                    Parameter(ParameterName, parameterValue);
                    break;
            }
        }

        /// <summary>
        /// 参数。
        /// </summary>
        /// <param name="parameterName">参数名称。</param>
        /// <param name="parameterValue">参数值。</param>
        public virtual void Parameter(string parameterName, object parameterValue)
        {
            if (parameterValue is null)
            {
                Null();

                return;
            }

            if (parameterValue is Version version)
            {
                parameterValue = version.ToString(4);
            }

            if (parameterName is null || parameterName.Length == 0)
            {
                parameterName = ParameterName;
            }

            string argName = parameterName;

            while (Parameters.TryGetValue(argName, out object data))
            {
                if (Equals(parameterValue, data))
                {
                    Write(settings.ParamterName(argName));

                    return;
                }

                argName = string.Concat(parameterName, "_", ParameterIndex.ToString());
            }

            Write(settings.ParamterName(argName));

            Parameters.Add(argName, parameterValue);
        }

        /// <summary>
        /// Select
        /// </summary>
        public void Select()
        {
            Write("SELECT");
            WhiteSpace();
        }

        /// <summary>
        /// Insert Into
        /// </summary>
        public void Insert()
        {
            Write("INSERT");
            WhiteSpace();
            Write("INTO");
            WhiteSpace();
        }

        private int usingIndex = 0;
        private readonly Stack<int> usingStack = new Stack<int>();

        /// <summary>
        /// 方法使用。
        /// </summary>
        /// <param name="action">方法。</param>
        /// <returns></returns>
        public string UsingAction(Action action)
        {
            int startIndex = sb.Length;

            if (usingIndex > 0)
            {
                usingStack.Push(startIndex);
            }

            usingIndex++;

            action.Invoke();

            int usingLength = usingStack.Count == usingIndex
                ? usingStack.Pop()
                : sb.Length;

            usingIndex--;

            return sb.ToString(startIndex, usingLength - startIndex);
        }

        /// <summary>
        /// Values
        /// </summary>
        public void Values() => Write("VALUES");

        /// <summary>
        /// Update
        /// </summary>
        public void Update()
        {
            Write("UPDATE");
            WhiteSpace();
        }

        /// <summary>
        /// Set
        /// </summary>
        public void Set()
        {
            WhiteSpace();
            Write("SET");
            WhiteSpace();
        }

        /// <summary>
        /// Delete
        /// </summary>
        public void Delete()
        {
            Write("DELETE");
            WhiteSpace();
        }

        /// <summary>
        /// Where
        /// </summary>
        public void Where()
        {
            WhiteSpace();
            Write("WHERE");
            WhiteSpace();
        }

        /// <summary>
        /// Having
        /// </summary>
        public void Having()
        {
            WhiteSpace();
            Write("HAVING");
            WhiteSpace();
        }

        /// <summary>
        /// And
        /// </summary>
        public void And()
        {
            WhiteSpace();
            Write(IsReverseCondition ? "OR" : "AND");
            WhiteSpace();
        }

        /// <summary>
        /// Or
        /// </summary>
        public void Or()
        {
            WhiteSpace();
            Write(IsReverseCondition ? "AND" : "OR");
            WhiteSpace();
        }

        /// <summary>
        /// Desc
        /// </summary>
        public void Descending()
        {
            WhiteSpace();
            Write("DESC");
        }

        /// <summary>
        /// Is
        /// </summary>
        public void Is()
        {
            WhiteSpace();
            Write("IS");
            WhiteSpace();
        }

        /// <summary>
        /// Not
        /// </summary>
        private void Not()
        {
            Write("NOT");
            WhiteSpace();
        }

        /// <summary>
        /// Null
        /// </summary>
        public void Null() => Write("NULL");

        /// <summary>
        /// {prefix}.
        /// </summary>
        /// <param name="prefix">字段前缀。</param>
        public virtual void Limit(string prefix)
        {
            if (prefix.IsNotEmpty())
            {
                Name(prefix);

                Write(".");
            }
        }

        /// <summary>
        /// 别名。
        /// </summary>
        /// <param name="name">名称。</param>
        public virtual void Alias(string name) => Write(writerMap.Name(name));

        /// <summary>
        /// AS
        /// </summary>
        public void As()
        {
            WhiteSpace();
            Write("AS");
            WhiteSpace();
        }

        /// <summary>
        /// AS {name}
        /// </summary>
        /// <param name="name">别名。</param>
        public virtual void As(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            As();

            Alias(name);
        }

        /// <summary>
        /// 字段。
        /// </summary>
        /// <param name="name">名称。</param>
        public virtual void Name(string name) => Write(writerMap.Name(name));

        /// <summary>
        /// {prefix}.{name}
        /// </summary>
        /// <param name="prefix">前缀。</param>
        /// <param name="name">字段。</param>
        public virtual void NameDot(string prefix, string name)
        {
            Limit(prefix);

            Name(name);
        }

        /// <summary>
        /// {name} {alias}
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="alias">别名。</param>
        public virtual void NameWhiteSpace(string name, string alias)
        {
            if (name.IndexOf('.') == -1)
            {
                Name(name);
            }
            else
            {
                var names = Split(name);

                for (int i = 0, len = names.Length; i < len; i++)
                {
                    Name(names[i]);

                    if (len > i)
                    {
                        Write(".");
                    }
                }
            }

            if (string.IsNullOrEmpty(alias))
            {
                return;
            }

            WhiteSpace();

            Alias(alias);
        }

        private static string[] Split(string name) //? 解决数据库名称包含【.】的问题。
        {
            List<string> names = new List<string>(3);

            if (TrySplit(name, '[', ']', names)) //? SQL Server
            {
                return names.ToArray();
            }

            if (TrySplit(name, '`', '`', names)) //? MySQL
            {
                return names.ToArray();
            }

            if (TrySplit(name, '"', '"', names)) //? Oracle
            {
                return names.ToArray();
            }

            return name.Split('.');
        }

        private static bool TrySplit(string name, char start, char end, List<string> names)
        {
            int indexOf = name.IndexOf(start);

            if (indexOf == -1)
            {
                return false;
            }

            int indexOfEnd = name.IndexOf(end, indexOf + 1);

            if (indexOfEnd == -1)
            {
                return false;
            }

            if (indexOf > 1)
            {
#if NETSTANDARD2_1_OR_GREATER
                names.AddRange(name[..(indexOf - 1)].Split(new char[1] { '.' }, StringSplitOptions.RemoveEmptyEntries));
#else
                names.AddRange(name.Substring(0, indexOf - 1).Split(new char[1] { '.' }, StringSplitOptions.RemoveEmptyEntries));
#endif
            }

            int charAtIndex;

            do
            {
                charAtIndex = indexOfEnd;

                names.Add(name.Substring(indexOf + 1, indexOfEnd - indexOf - 1));

                indexOf = name.IndexOf(start, indexOfEnd + 1);

                if (indexOf == -1)
                {
                    break;
                }

                indexOfEnd = name.IndexOf(end, indexOf + 1);

            } while (indexOfEnd > -1);

            if (name.Length > (charAtIndex + 2))
            {
#if NETSTANDARD2_1_OR_GREATER
                names.AddRange(name[(charAtIndex + 2)..].Split(new char[1] { '.' }, StringSplitOptions.RemoveEmptyEntries));
#else
                names.AddRange(name.Substring(charAtIndex + 2).Split(new char[1] { '.' }, StringSplitOptions.RemoveEmptyEntries));
#endif
            }

            return true;
        }

        /// <summary>
        /// “ ”
        /// </summary>
        public void WhiteSpace() => Write(writerMap.WhiteSpace);

        /// <summary>
        /// IS NULL
        /// </summary>
        public void IsNull()
        {
            Is();

            if (IsReverseCondition)
            {
                Not();
            }

            Null();
        }

        /// <summary>
        /// IS NOT ULL
        /// </summary>
        public void IsNotNull()
        {
            Is();

            if (!IsReverseCondition)
            {
                Not();
            }

            Null();
        }

        /// <summary>
        /// 长度函数。
        /// </summary>
        public void LengthMethod() => Write(settings.Length);

        /// <summary>
        /// 索引函数。
        /// </summary>
        public void IndexOfMethod() => Write(settings.IndexOf);

        /// <summary>
        /// 截取函数。
        /// </summary>
        public void SubstringMethod() => Write(settings.Substring);

        /// <summary>
        /// 写入排序内容。
        /// </summary>
        /// <param name="action">方法。</param>
        public void UsingSort(Action action)
        {
            writeOrderBy = true;

            action.Invoke();

            writeOrderBy = false;
        }

        /// <summary>
        /// 写入内容。
        /// </summary>
        /// <param name="value">内容。</param>
        public void Write(string value)
        {
            if (value == null || value.Length == 0)
            {
                return;
            }

            if (writeOrderBy)
            {
                if (appendOrderByAt > -1)
                {
                    sbOrder.Insert(appendOrderByAt, value);

                    appendOrderByAt += value.Length;
                }
                else
                {
                    sbOrder.Append(value);
                }
            }
            else if (appendAt > -1)
            {
                sb.Insert(appendAt, value);

                appendAt += value.Length;
            }
            else
            {
                sb.Append(value);
            }
        }

        /// <summary>
        /// 写入类型。
        /// </summary>
        /// <param name="nodeType">节点类型。</param>
        public void Write(ExpressionType nodeType) => Write(ExpressionExtensions.GetOperator(IsReverseCondition ? nodeType.ReverseWhere() : nodeType));

        /// <summary>
        /// 写入换行。
        /// </summary>
        public void WriteLine() => Write(Environment.NewLine);

        /// <summary>
        /// =
        /// </summary>
        public void Equal() => Write(ExpressionType.Equal);

        /// <summary>
        /// !=
        /// </summary>
        public void NotEqual() => Write(ExpressionType.NotEqual);

        /// <summary>
        /// 条件反转。
        /// </summary>
        /// <param name="reverseCondition">方法。</param>
        public void ReverseCondition(Action reverseCondition)
        {
            IsReverseCondition ^= true;

            reverseCondition.Invoke();

            IsReverseCondition ^= true;
        }

        /// <summary>
        /// 条件反转。
        /// </summary>
        /// <param name="reverseCondition">方法。</param>
        public void ReverseCondition<T>(Func<T> reverseCondition)
        {
            IsReverseCondition ^= true;

            reverseCondition.Invoke();

            IsReverseCondition ^= true;
        }

        /// <summary>
        /// false
        /// </summary>
        public void BooleanFalse()
        {
            Parameter("__variable_false", false);
        }

        /// <summary>
        /// true
        /// </summary>
        public void BooleanTrue()
        {
            Parameter("__variable_true", true);
        }

        /// <summary>
        /// 返回写入器数据。
        /// </summary>
        /// <param name="startIndex">开始位置。</param>
        /// <param name="length">长度。</param>
        /// <returns></returns>
        public string ToString(int startIndex, int length) => sb.ToString(startIndex, length);

        /// <summary>
        /// 返回写入器数据。
        /// </summary>
        public override string ToString() => sb.ToString();

        /// <summary>
        /// 返回SQL。
        /// </summary>
        /// <returns></returns>
        public virtual string ToSQL() => string.Concat(sb.ToString(), sbOrder.ToString());

        /// <summary>
        /// 返回SQL。
        /// </summary>
        /// <returns></returns>
        public virtual string ToSQL(int take, int skip)
        {
            if (take > 0 || skip > 0)
            {
                return settings.ToSQL(sb.ToString(), Math.Max(take, 0), Math.Max(skip, 0), sbOrder.ToString());
            }

            return ToSQL();
        }
    }
}

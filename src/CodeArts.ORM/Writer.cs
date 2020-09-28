using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace CodeArts.ORM
{
    /// <summary>
    /// SQL写入流
    /// </summary>
    public class Writer
    {
        private readonly IWriterMap writer;

        private readonly StringBuilder sb;

        private readonly StringBuilder sbOrder;

        private readonly List<StringBuilder> builders = new List<StringBuilder>();

        private readonly ISQLCorrectSettings settings;

        private bool writeOrderBy = false;

        private int parameterIndex = 0;

        /// <summary>
        /// 参数名称
        /// </summary>
        protected virtual string ParameterName => string.Concat("__variable_", ParameterIndex.ToString());

        /// <summary>
        /// 参数索引。
        /// </summary>
        protected virtual int ParameterIndex => ++parameterIndex;

        private int appendAt = -1;

        private int appendOrderByAt = -1;

        /// <summary>
        /// 写入位置
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
        /// 内容长度
        /// </summary>
        public int Length => sb.Length;
        /// <summary>
        /// 条件取反。
        /// </summary>
        public bool ReverseCondition { get; internal set; }
        /// <summary>
        /// 移除数据。
        /// </summary>
        /// <param name="index">索引开始位置</param>
        /// <param name="lenght">移除字符长度</param>
        public void Remove(int index, int lenght) => sb.Remove(index, lenght);

        private Dictionary<string, object> parameters;
        /// <summary>
        /// 参数集合。
        /// </summary>
        public Dictionary<string, object> Parameters { internal set => parameters = value; get => parameters ?? (parameters = new Dictionary<string, object>()); }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settings">SQL矫正配置。</param>
        /// <param name="writer">写入配置。</param>
        /// <param name="parameters">参数。</param>
        public Writer(ISQLCorrectSettings settings, IWriterMap writer, Dictionary<string, object> parameters)
        {
            sb = new StringBuilder();

            sbOrder = new StringBuilder();

            this.parameterIndex = parameters?.Count ?? 0;

            this.parameters = parameters ?? new Dictionary<string, object>();

            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));

            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// )
        /// </summary>
        public void CloseBrace() => Write(writer.CloseBrace);

        /// <summary>
        /// ,
        /// </summary>
        public void Delimiter() => Write(writer.Delimiter);

        /// <summary>
        /// DISTINCT
        /// </summary>
        public void Distinct() => Write("DISTINCT" + writer.WhiteSpace);

        /// <summary>
        /// ''
        /// </summary>
        public void EmptyString() => Write(writer.EmptyString);

        /// <summary>
        /// Exists
        /// </summary>
        public void Exists()
        {
            if (ReverseCondition)
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

            if (ReverseCondition)
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

            if (ReverseCondition)
            {
                Not();
            }

            Write("IN");
        }

        /// <summary>
        /// From
        /// </summary>
        public void From() => Write(writer.WhiteSpace + "FROM" + writer.WhiteSpace);

        /// <summary>
        /// Left Join
        /// </summary>
        public void Join() => Write(writer.WhiteSpace + "LEFT" + writer.WhiteSpace + "JOIN" + writer.WhiteSpace);

        /// <summary>
        /// (
        /// </summary>
        public void OpenBrace() => Write(writer.OpenBrace);

        /// <summary>
        /// Order By
        /// </summary>
        public void OrderBy() => Write(writer.WhiteSpace + "ORDER" + writer.WhiteSpace + "BY" + writer.WhiteSpace);

        /// <summary>
        /// 参数
        /// </summary>
        /// <param name="parameterValue">参数值</param>
        public virtual void Parameter(object parameterValue)
        {
            if (parameterValue == null)
            {
                Null();

                return;
            }

            foreach (var kv in Parameters)
            {
                if (kv.Value == parameterValue)
                {
                    Write(settings.ParamterName(kv.Key));

                    return;
                }
            }

            string parameterName = ParameterName;

            while (Parameters.ContainsKey(parameterName))
            {
                parameterName = ParameterName;
            }

            Write(settings.ParamterName(parameterName));

            Parameters.Add(parameterName, parameterValue);
        }

        /// <summary>
        /// 参数
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="parameterValue">参数值</param>
        public void Parameter(string parameterName, object parameterValue)
        {
            if (parameterValue == null)
            {
                Null();

                return;
            }

            if (parameterName == null || parameterName.Length == 0)
            {
                Parameter(parameterValue);

                return;
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
        public void Select() => Write("SELECT" + writer.WhiteSpace);

        /// <summary>
        /// Insert Into
        /// </summary>
        public void Insert() => Write("INSERT" + writer.WhiteSpace + "INTO" + writer.WhiteSpace);

        /// <summary>
        /// Values
        /// </summary>
        public void Values() => Write("VALUES");

        /// <summary>
        /// Update
        /// </summary>
        public void Update() => Write("UPDATE" + writer.WhiteSpace);

        /// <summary>
        /// Set
        /// </summary>
        public void Set() => Write(writer.WhiteSpace + "SET" + writer.WhiteSpace);

        /// <summary>
        /// Delete
        /// </summary>
        public void Delete() => Write("DELETE" + writer.WhiteSpace);

        /// <summary>
        /// Where
        /// </summary>
        public void Where() => Write(writer.WhiteSpace + "WHERE" + writer.WhiteSpace);

        /// <summary>
        /// And
        /// </summary>
        public void And() => Write(writer.WhiteSpace + (ReverseCondition ? "OR" : "AND") + writer.WhiteSpace);

        /// <summary>
        /// Or
        /// </summary>
        public void Or() => Write(writer.WhiteSpace + (ReverseCondition ? "AND" : "OR") + writer.WhiteSpace);

        /// <summary>
        /// Desc
        /// </summary>
        public void Descending() => Write(writer.WhiteSpace + "DESC");

        /// <summary>
        /// Is
        /// </summary>
        public void Is() => Write(writer.WhiteSpace + "IS" + writer.WhiteSpace);

        /// <summary>
        /// Not
        /// </summary>
        public void Not() => Write("NOT" + writer.WhiteSpace);

        /// <summary>
        /// Null
        /// </summary>
        public virtual void Null() => Write("NULL");

        /// <summary>
        /// {prefix}.
        /// </summary>
        /// <param name="prefix">字段前缀</param>
        public void Limit(string prefix)
        {
            if (prefix.IsNotEmpty())
            {
                Name(prefix);

                Write(".");
            }
        }

        /// <summary>
        /// 别名
        /// </summary>
        /// <param name="name">名称</param>
        public void Alias(string name) => Write(writer.Name(name));

        /// <summary>
        /// AS
        /// </summary>
        public void As() => Write(writer.WhiteSpace + "AS" + writer.WhiteSpace);

        /// <summary>
        /// AS {name}
        /// </summary>
        /// <param name="name">别名</param>
        public void As(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            As();

            Alias(name);
        }

        /// <summary>
        /// 字段
        /// </summary>
        /// <param name="name">名称</param>
        public void Name(string name) => Write(writer.Name(name));

        /// <summary>
        /// 空格
        /// </summary>
        public void WhiteSpace() => Write(writer.WhiteSpace);

        /// <summary>
        /// {prefix}.{name}
        /// </summary>
        /// <param name="prefix">前缀</param>
        /// <param name="name">字段</param>
        public void Name(string prefix, string name)
        {
            Limit(prefix);

            Name(name);
        }

        /// <summary>
        /// IS NULL
        /// </summary>
        public void IsNull()
        {
            Is();

            if (ReverseCondition)
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

            if (!ReverseCondition)
            {
                Not();
            }

            Null();
        }

        /// <summary>
        /// 长度函数
        /// </summary>
        public void LengthMethod() => Write(settings.Length);

        /// <summary>
        /// 索引函数
        /// </summary>
        public void IndexOfMethod() => Write(settings.IndexOf);

        /// <summary>
        /// 截取函数
        /// </summary>
        public void SubstringMethod() => Write(settings.Substring);

        /// <summary>
        /// {name} {alias}
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="alias">别名</param>
        public void TableName(string name, string alias)
        {
            Name(name);

            if (string.IsNullOrEmpty(alias))
            {
                return;
            }

            WhiteSpace();

            Alias(alias);
        }

        /// <summary>
        /// 添加写入器(有数据变化的时候或写入到写入器中)。
        /// </summary>
        /// <param name="builder">写入器。</param>
        internal void AddWriter(StringBuilder builder) => builders.Add(builder ?? throw new ArgumentNullException(nameof(builder)));

        /// <summary>
        /// 移除写入器。
        /// </summary>
        /// <param name="builder">写入器。</param>
        internal void RemoveWriter(StringBuilder builder) => builders.Remove(builder);

        /// <summary>
        /// 清空写入器。
        /// </summary>
        public void ClearWriter() => builders.Clear();

        /// <summary>
        /// 写入排序内容。
        /// </summary>
        /// <param name="action"></param>
        public void UsingSort(Action action)
        {
            writeOrderBy = true;

            action.Invoke();

            writeOrderBy = false;
        }

        /// <summary>
        /// 写入内容。
        /// </summary>
        /// <param name="value">内容</param>
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
        /// <param name="nodeType">节点类型</param>
        public void Write(ExpressionType nodeType)
        {
            Write(ExpressionExtensions.GetOperator(ReverseCondition ? nodeType.ReverseWhere() : nodeType));
        }

        /// <summary>
        /// 写入类型。
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        /// <param name="left">左节点类型</param>
        /// <param name="right">右节点类型</param>
        public virtual void WriteAdd(ExpressionType nodeType, Type left, Type right)
        {
            if (settings.Engine == DatabaseEngine.Oracle)
            {
                Write(" || ");
            }
            else
            {
                Write(ExpressionExtensions.GetOperator(ReverseCondition ? nodeType.ReverseWhere() : nodeType));
            }
        }

        /// <summary>
        /// =
        /// </summary>
        public void Equal() => Write(ExpressionType.Equal);

        /// <summary>
        /// &lt;&gt;
        /// </summary>
        public void NotEqual() => Write(ExpressionType.NotEqual);

        /// <summary>
        /// false
        /// </summary>
        public virtual void BooleanFalse()
        {
            Parameter("__variable_false", false);
        }

        /// <summary>
        /// true
        /// </summary>
        public virtual void BooleanTrue()
        {
            Parameter("__variable_true", true);
        }

        /// <summary>
        /// 返回写入器数据。
        /// </summary>
        /// <param name="startIndex">开始位置</param>
        /// <param name="length">长度</param>
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
                return settings.ToSQL(sb.ToString(), take, skip, sbOrder.ToString());
            }

            return ToSQL();
        }
    }
}

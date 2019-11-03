using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// SQL写入流
    /// </summary>
    public class Writer
    {
        private readonly IWriterMap _writer;

        private readonly StringBuilder _builder;

        private readonly List<StringBuilder> builders = new List<StringBuilder>();

        private readonly ISQLCorrectSettings settings;

        private int ParameterIndex = 0;
        /// <summary>
        /// 参数名称
        /// </summary>
        protected virtual string ParameterName
        {
            get
            {
                return string.Concat("__variable_", ParameterIndex += 1);
            }
        }

        /// <summary>
        /// 写入位置
        /// </summary>
        internal int AppendAt = -1;
        /// <summary>
        /// 有写入器时，不向当前写入器写入内容。
        /// </summary>
        internal bool HasWriteReturn { get; set; }

        /// <summary>
        /// 内容长度
        /// </summary>
        public int Length => _builder.Length;
        /// <summary>
        /// 条件取反。
        /// </summary>
        public bool Not { get; internal set; }
        /// <summary>
        /// 移除数据。
        /// </summary>
        /// <param name="index">索引开始位置</param>
        /// <param name="lenght">移除字符长度</param>
        public void Remove(int index, int lenght) => _builder.Remove(index, lenght);

        private Dictionary<string, object> paramsters;
        /// <summary>
        /// 参数集合。
        /// </summary>
        public Dictionary<string, object> Parameters { internal set => paramsters = value; get => paramsters ?? (paramsters = new Dictionary<string, object>()); }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settings">SQL矫正配置</param>
        /// <param name="writer">写入配置</param>
        public Writer(ISQLCorrectSettings settings, IWriterMap writer)
        {
            _builder = new StringBuilder();

            _writer = writer ?? throw new ArgumentNullException(nameof(writer));

            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
        /// <summary>
        /// )
        /// </summary>
        public void CloseBrace() => Write(_writer.CloseBrace);
        /// <summary>
        /// ,
        /// </summary>
        public void Delimiter() => Write(_writer.Delimiter);
        /// <summary>
        /// DISTINCT
        /// </summary>
        public void Distinct() => Write("DISTINCT" + _writer.WhiteSpace);
        /// <summary>
        /// ''
        /// </summary>
        public void EmptyString() => Write(_writer.EmptyString);
        /// <summary>
        /// Exists
        /// </summary>
        public void Exists()
        {
            if (Not) WriteNot();

            Write("EXISTS");
        }
        /// <summary>
        /// Like
        /// </summary>
        public void Like()
        {
            WhiteSpace();

            if (Not) WriteNot();

            Write("LIKE");

            WhiteSpace();
        }
        /// <summary>
        /// IN
        /// </summary>
        public void Contains()
        {
            WhiteSpace();

            if (Not) WriteNot();

            Write("IN");
        }
        /// <summary>
        /// From
        /// </summary>
        public void From() => Write(_writer.WhiteSpace + "FROM" + _writer.WhiteSpace);
        /// <summary>
        /// Left Join
        /// </summary>
        public void Join() => Write(_writer.WhiteSpace + "LEFT" + _writer.WhiteSpace + "JOIN" + _writer.WhiteSpace);
        /// <summary>
        /// (
        /// </summary>
        public void OpenBrace() => Write(_writer.OpenBrace);
        /// <summary>
        /// Order By
        /// </summary>
        public void OrderBy() => Write(_writer.WhiteSpace + "ORDER" + _writer.WhiteSpace + "BY" + _writer.WhiteSpace);
        /// <summary>
        /// 参数
        /// </summary>
        /// <param name="value">参数值</param>
        public virtual void Parameter(object value)
        {
            if (value == null)
            {
                WriteNull();
                return;
            }

            string paramterName = settings.ParamterName(ParameterName);

            Write(paramterName);

            Parameters.Add(paramterName, value);
        }
        /// <summary>
        /// 参数
        /// </summary>
        /// <param name="name">参数名称</param>
        /// <param name="value">参数值</param>
        public void Parameter(string name, object value)
        {
            if (value == null)
            {
                WriteNull();
                return;
            }

            if (name == null || name.Length == 0)
            {
                Parameter(value);
                return;
            }

            string paramterName = settings.ParamterName(name);

            if (!Parameters.TryGetValue(paramterName, out object data))
            {
                Write(paramterName);

                Parameters.Add(paramterName, value);

                return;
            }

            if (Equals(value, data))
            {
                Write(paramterName);

                return;
            }

            paramterName = settings.ParamterName(string.Concat(name, ParameterIndex += 1));

            Write(paramterName);

            Parameters.Add(paramterName, value);
        }
        /// <summary>
        /// Select
        /// </summary>
        public void Select() => Write("SELECT" + _writer.WhiteSpace);
        /// <summary>
        /// Insert Into
        /// </summary>
        public void Insert() => Write("INSERT" + _writer.WhiteSpace + "INTO" + _writer.WhiteSpace);

        /// <summary>
        /// Values
        /// </summary>
        public void Values() => Write("VALUES");
        /// <summary>
        /// Update
        /// </summary>
        public void Update() => Write("UPDATE" + _writer.WhiteSpace);
        /// <summary>
        /// Set
        /// </summary>
        public void Set() => Write(_writer.WhiteSpace + "SET" + _writer.WhiteSpace);
        /// <summary>
        /// Delete
        /// </summary>
        public void Delete() => Write("DELETE" + _writer.WhiteSpace);
        /// <summary>
        /// Where
        /// </summary>
        public void Where() => Write(_writer.WhiteSpace + "WHERE" + _writer.WhiteSpace);
        /// <summary>
        /// And
        /// </summary>
        public void WriteAnd() => Write(_writer.WhiteSpace + (Not ? "OR" : "AND") + _writer.WhiteSpace);
        /// <summary>
        /// Desc
        /// </summary>
        public void WriteDesc() => Write(_writer.WhiteSpace + "DESC");
        /// <summary>
        /// Is
        /// </summary>
        public void WriteIS() => Write(_writer.WhiteSpace + "IS" + _writer.WhiteSpace);
        /// <summary>
        /// Not
        /// </summary>
        public void WriteNot() => Write("NOT" + _writer.WhiteSpace);
        /// <summary>
        /// Null
        /// </summary>
        public virtual void WriteNull() => Write("NULL");
        /// <summary>
        /// Or
        /// </summary>
        public void WriteOr() => Write(_writer.WhiteSpace + (Not ? "AND" : "OR") + _writer.WhiteSpace);
        /// <summary>
        /// {prefix}.
        /// </summary>
        /// <param name="prefix">字段前缀</param>
        public void WritePrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return;

            Name(prefix);

            Write(".");
        }
        /// <summary>
        /// 别名
        /// </summary>
        /// <param name="name"></param>
        public void Alias(string name) => Write(_writer.Alias(name));
        /// <summary>
        /// AS
        /// </summary>
        public void As() => Write(_writer.WhiteSpace + "AS" + _writer.WhiteSpace);
        /// <summary>
        /// AS {name}
        /// </summary>
        /// <param name="name">别名</param>
        public void As(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            As();

            Alias(name);
        }
        /// <summary>
        /// 字段
        /// </summary>
        /// <param name="name">名称</param>
        public void Name(string name) => Write(_writer.Name(name));
        /// <summary>
        /// 表名称
        /// </summary>
        /// <param name="name">名称</param>
        public void TableName(string name) => Write(_writer.TableName(name));
        /// <summary>
        /// 空格
        /// </summary>
        public void WhiteSpace() => Write(_writer.WhiteSpace);
        /// <summary>
        /// {prefix}.{name}
        /// </summary>
        /// <param name="prefix">前缀</param>
        /// <param name="name">字段</param>
        public void Name(string prefix, string name)
        {
            WritePrefix(prefix);

            Name(name);
        }
        /// <summary>
        /// IS NULL
        /// </summary>
        public void IsNull()
        {
            WriteIS();

            if (Not)
            {
                WriteNot();
            }

            WriteNull();
        }
        /// <summary>
        /// IS NOT ULL
        /// </summary>
        public void IsNotNull()
        {
            WriteIS();

            if (!Not)
            {
                WriteNot();
            }

            WriteNull();
        }
        /// <summary>
        /// 长度函数
        /// </summary>
        public void LengthMethod()
        {
            Write(settings.Length);
        }
        /// <summary>
        /// 索引函数
        /// </summary>
        public void IndexOfMethod()
        {
            Write(settings.IndexOf);
        }
        /// <summary>
        /// 截取函数
        /// </summary>
        public void SubstringMethod()
        {
            Write(settings.Substring);
        }
        /// <summary>
        /// {name} {alias}
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="alias">别名</param>
        public void TableName(string name, string alias)
        {
            TableName(name);

            if (string.IsNullOrEmpty(alias)) return;

            WhiteSpace();

            Alias(alias);
        }

        /// <summary>
        /// 添加写入器(有数据变化的时候或写入到写入器中)。
        /// </summary>
        /// <param name="builder">写入器。</param>
        public void AddWriter(StringBuilder builder) => builders.Add(builder ?? throw new ArgumentNullException(nameof(builder)));
        /// <summary>
        /// 移除写入器。
        /// </summary>
        /// <param name="builder">写入器。</param>
        public void RemoveWriter(StringBuilder builder) => builders.Remove(builder);
        /// <summary>
        /// 清空写入器。
        /// </summary>
        public void ClearWriter() => builders.Clear();

        /// <summary>
        /// 写入内容。
        /// </summary>
        /// <param name="value">内容</param>
        public void Write(string value)
        {
            if (value == null || value.Length == 0)
                return;

            if (builders.Count > 0)
            {
                builders.ForEach(writer => writer.Append(value));

                if (HasWriteReturn) return;
            }

            if (AppendAt > -1)
            {
                _builder.Insert(AppendAt, value);
                AppendAt += value.Length;
            }
            else
            {
                _builder.Append(value);
            }
        }
        /// <summary>
        /// 写入类型。
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        public void Write(ExpressionType nodeType)
        {
            Write(ExpressionExtensions.GetOperator(Not ? nodeType.ReverseWhere() : nodeType));
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
            if (Not)
            {
                Parameter("__variable_true", true);
            }
            else
            {
                Parameter("__variable_false", false);
            }
        }
        /// <summary>
        /// true
        /// </summary>
        public virtual void BooleanTrue()
        {
            if (Not)
            {
                Parameter("__variable_false", false);
            }
            else
            {
                Parameter("__variable_true", true);
            }

        }

        /// <summary>
        /// 返回写入器数据。
        /// </summary>
        /// <param name="startIndex">开始位置</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        public string ToString(int startIndex, int length) => _builder.ToString(startIndex, length);
        /// <summary>
        /// 返回写入器数据。
        /// </summary>
        public override string ToString() => _builder.ToString();
    }
}

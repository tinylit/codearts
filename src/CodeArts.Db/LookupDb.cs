using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Db
{
    /// <summary>
    /// 查找数据类型。
    /// </summary>
    public class LookupDb
    {
        private static readonly Dictionary<Type, DbType> typeMap;

        static LookupDb()
        {
            typeMap = new Dictionary<Type, DbType>
            {
                [typeof(byte)] = DbType.Byte,
                [typeof(sbyte)] = DbType.SByte,
                [typeof(short)] = DbType.Int16,
                [typeof(ushort)] = DbType.UInt16,
                [typeof(int)] = DbType.Int32,
                [typeof(uint)] = DbType.UInt32,
                [typeof(long)] = DbType.Int64,
                [typeof(ulong)] = DbType.UInt64,
                [typeof(float)] = DbType.Single,
                [typeof(double)] = DbType.Double,
                [typeof(decimal)] = DbType.Decimal,
                [typeof(bool)] = DbType.Boolean,
                [typeof(string)] = DbType.String,
                [typeof(char)] = DbType.StringFixedLength,
                [typeof(Guid)] = DbType.Guid,
                [typeof(DateTime)] = DbType.DateTime,
                [typeof(DateTimeOffset)] = DbType.DateTimeOffset,
                [typeof(TimeSpan)] = DbType.Time,
                [typeof(byte[])] = DbType.Binary,
                [typeof(object)] = DbType.Object
            };
        }

        /// <summary>
        /// 转数据库类型。
        /// </summary>
        /// <param name="dataType">类型。</param>
        /// <returns></returns>
        public static DbType For(Type dataType)
        {
            if (dataType.IsNullable())
            {
                dataType = Nullable.GetUnderlyingType(dataType);
            }

            if (dataType.IsEnum)
            {
                dataType = Enum.GetUnderlyingType(dataType);
            }

            if (typeMap.TryGetValue(dataType, out DbType dbType))
            {
                return dbType;
            }

            if (dataType.FullName == "System.Data.Linq.Binary")
            {
                return DbType.Binary;
            }

            return DbType.Object;
        }

        /// <summary>
        /// 参数适配。
        /// </summary>
        /// <param name="command">命令。</param>
        /// <param name="name">参数名称。</param>
        /// <param name="value">参数值。</param>
        public static void AddParameterAuto(IDbCommand command, string name, ParameterValue value)
        {
            var dbParameter = command.CreateParameter();

            dbParameter.Value = value.IsNull ? DBNull.Value : value.Value;
            dbParameter.ParameterName = name;
            dbParameter.Direction = ParameterDirection.Input;
            dbParameter.DbType = For(value.ValueType);

            command.Parameters.Add(dbParameter);
        }

        /// <summary>
        /// 参数适配。
        /// </summary>
        /// <param name="command">命令。</param>
        /// <param name="name">参数名称。</param>
        /// <param name="value">参数值。</param>
        public static void AddParameterAuto(IDbCommand command, string name, object value)
        {
            var dbParameter = command.CreateParameter();

            switch (value)
            {
                case IDbDataParameter dbDataParameter when dbParameter is IDbDataParameter parameter:

                    parameter.Value = dbDataParameter.Value;
                    parameter.ParameterName = name;
                    parameter.Direction = dbDataParameter.Direction;
                    parameter.DbType = dbDataParameter.DbType;
                    parameter.SourceColumn = dbDataParameter.SourceColumn;
                    parameter.SourceVersion = dbDataParameter.SourceVersion;

                    if (dbParameter is System.Data.Common.DbParameter myParameter)
                    {
                        myParameter.IsNullable = dbDataParameter.IsNullable;
                    }

                    parameter.Scale = dbDataParameter.Scale;
                    parameter.Size = dbDataParameter.Size;
                    parameter.Precision = dbDataParameter.Precision;

                    command.Parameters.Add(dbParameter);
                    break;
                case IDataParameter dataParameter:
                    dbParameter.Value = dataParameter.Value;
                    dbParameter.ParameterName = name;
                    dbParameter.Direction = dataParameter.Direction;
                    dbParameter.DbType = dataParameter.DbType;
                    dbParameter.SourceColumn = dataParameter.SourceColumn;
                    dbParameter.SourceVersion = dataParameter.SourceVersion;
                    break;
                case ParameterValue parameterValue:
                    dbParameter.Value = parameterValue.IsNull ? DBNull.Value : parameterValue.Value;
                    dbParameter.ParameterName = name;
                    dbParameter.Direction = ParameterDirection.Input;
                    dbParameter.DbType = For(parameterValue.ValueType);
                    break;
                default:
                    dbParameter.Value = value ?? DBNull.Value;
                    dbParameter.ParameterName = name;
                    dbParameter.Direction = ParameterDirection.Input;
                    dbParameter.DbType = value is null ? DbType.Object : For(value.GetType());
                    break;
            }

            command.Parameters.Add(dbParameter);
        }
    }
}

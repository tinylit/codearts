using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据库参数集合。
    /// </summary>
    public class DbParameterCollection : IDataParameterCollection
    {
        private readonly IDataParameterCollection parameters;
        private readonly ISQLCorrectSettings settings;

        /// <inheritdoc />
        internal DbParameterCollection(IDataParameterCollection parameters, ISQLCorrectSettings settings)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <inheritdoc />
        public object this[string parameterName]
        {
            get => parameters[GetParameterName(parameterName)];
            set
            {
                if (value is IDataParameter dataParameter)
                {
                    dataParameter.ParameterName = GetParameterName(dataParameter.ParameterName);
                }

                parameters[GetParameterName(parameterName)] = value;
            }
        }
        object IList.this[int index]
        {
            get => parameters[index];
            set
            {
                if (value is IDataParameter dataParameter)
                {
                    dataParameter.ParameterName = GetParameterName(dataParameter.ParameterName);
                }

                parameters[index] = value;
            }
        }

        /// <inheritdoc />
        public int Count => parameters.Count;

        /// <inheritdoc />
        public bool IsReadOnly => parameters.IsReadOnly;
        /// <inheritdoc />
        public bool IsFixedSize => parameters.IsFixedSize;
        /// <inheritdoc />
        public object SyncRoot => parameters.SyncRoot;
        /// <inheritdoc />
        public bool IsSynchronized => parameters.IsSynchronized;

        private string GetParameterName(string parameterName)
        {
            switch (parameterName[0])
            {
                case '@':
                case ':':
                case '?':
                    return settings.ParamterName(parameterName.Substring(1));
                default:
                    return settings.ParamterName(parameterName);
            }
        }

        /// <inheritdoc />
        public int Add(object value)
        {
            if (value is IDataParameter dataParameter)
            {
                dataParameter.ParameterName = GetParameterName(dataParameter.ParameterName);
            }

            return parameters.Add(value);
        }

        /// <inheritdoc />
        public void Clear() => parameters.Clear();

        /// <inheritdoc />
        public bool Contains(string parameterName) => parameters.Contains(GetParameterName(parameterName));

        /// <inheritdoc />
        public bool Contains(object value) => parameters.Contains(value);
        /// <inheritdoc />
        public void CopyTo(Array array, int index) => parameters.CopyTo(array, index);
        /// <inheritdoc />
        public IEnumerator GetEnumerator() => parameters.GetEnumerator();
        /// <inheritdoc />
        public int IndexOf(string parameterName) => parameters.IndexOf(GetParameterName(parameterName));
        /// <inheritdoc />
        public int IndexOf(object value) => parameters.IndexOf(value);
        /// <inheritdoc />
        public void Insert(int index, object value)
        {
            if (value is IDataParameter dataParameter)
            {
                dataParameter.ParameterName = GetParameterName(dataParameter.ParameterName);
            }

            parameters.Insert(index, value);
        }
        /// <inheritdoc />
        public void Remove(object value) => parameters.Remove(value);
        /// <inheritdoc />
        public void RemoveAt(string parameterName) => parameters.RemoveAt(GetParameterName(parameterName));
        /// <inheritdoc />
        public void RemoveAt(int index) => parameters.RemoveAt(index);
    }
}

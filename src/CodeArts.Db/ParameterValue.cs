using System;

namespace CodeArts.Db
{
    /// <summary>
    /// 数据。
    /// </summary>
    public struct ParameterValue : IEquatable<ParameterValue>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value">值。</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/>is null.</exception>
        public ParameterValue(object value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            ValueType = value.GetType();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="valueType">值类型。</param>
        /// <exception cref="ArgumentNullException"><paramref name="valueType"/>is null.</exception>
        public ParameterValue(Type valueType)
        {
            Value = null;

            ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        }

        /// <summary>
        /// 是否为空。
        /// </summary>
        public bool IsNull => Value is null;

        /// <summary>
        /// 值。
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// 值类型。
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(byte value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(int value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(long value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(float value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(double value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(decimal value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(DateTime value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(byte? value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(int? value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(long? value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(float? value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(double? value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(decimal? value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(DateTime? value) => new ParameterValue(value);

        /// <summary>
        /// 参数值。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ParameterValue(string value) => value is null ? new ParameterValue(typeof(string)) : new ParameterValue(value);

        /// <summary>
        /// 创建参数值。
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="valueType">值的类型。</param>
        /// <exception cref="ArgumentException"><paramref name="value"/>和<paramref name="valueType"/>同时为null。</exception>
        /// <exception cref="NotSupportedException"><paramref name="value"/>和<paramref name="valueType"/>都不为null，且<paramref name="value"/>无法转换为<paramref name="valueType"/>类型。</exception>
        /// <returns></returns>
        public static ParameterValue Create(object value, Type valueType)
        {
            if (value is null)
            {
                if (valueType is null)
                {
                    throw new ArgumentException();
                }

                return new ParameterValue(valueType);
            }

            if (valueType is null)
            {
                return new ParameterValue(value);
            }

            if (valueType == value.GetType())
            {
                return new ParameterValue(value);
            }

            return new ParameterValue(Mapper.Cast(value, valueType));
        }

        /// <summary>
        /// 是否相同。
        /// </summary>
        /// <param name="other">额外的。</param>
        /// <returns></returns>
        public bool Equals(ParameterValue other) => other.ValueType == ValueType && ReferenceEquals(other.Value, Value);
    }
}

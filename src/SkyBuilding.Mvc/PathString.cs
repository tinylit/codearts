#if  NET40 || NET45 || NET451 || NET452 || NET461
using System;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace SkyBuilding.Mvc
{
    /// <summary>
    /// Provides correct escaping for Path and PathBase values when needed to reconstruct a request or redirect URI string
    /// </summary>
    [TypeConverter(typeof(PathStringConverter))]
    public struct PathString : IEquatable<PathString>
    {
        private class PathStringConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (!(sourceType == typeof(string)))
                {
                    return base.CanConvertFrom(context, sourceType);
                }
                return true;
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (!(value is string))
                {
                    return base.ConvertFrom(context, culture, value);
                }
                return PathString.ConvertFromString((string)value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (!(destinationType == typeof(string)))
                {
                    return base.ConvertTo(context, culture, value, destinationType);
                }
                return value.ToString();
            }
        }

        private class PathStringHelper
        {
            private static readonly bool[] ValidPathChars = new bool[128]
            {
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                false,
                false,
                true,
                false,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                false,
                true,
                false,
                false,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                false,
                false,
                false,
                false,
                true,
                false,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                false,
                false,
                false,
                true,
                false
            };

            public static bool IsValidPathChar(char c)
            {
                if (c < ValidPathChars.Length)
                {
                    return ValidPathChars[c];
                }
                return false;
            }

            public static bool IsPercentEncodedChar(string str, int index)
            {
                if (index < str.Length - 2 && str[index] == '%' && IsHexadecimalChar(str[index + 1]))
                {
                    return IsHexadecimalChar(str[index + 2]);
                }
                return false;
            }

            public static bool IsHexadecimalChar(char c)
            {
                if (('0' > c || c > '9') && ('A' > c || c > 'F'))
                {
                    if ('a' <= c)
                    {
                        return c <= 'f';
                    }
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Represents the empty path. This field is read-only.
        /// </summary>
        public static readonly PathString Empty = new PathString(string.Empty);

        private readonly string _value;

        /// <summary>
        /// The unescaped path value
        /// </summary>
        public string Value => _value;

        /// <summary>
        /// True if the path is not empty
        /// </summary>
        public bool HasValue => !string.IsNullOrEmpty(_value);

        /// <summary>
        /// Initialize the path string with a given value. This value must be in unescaped format. Use
        /// PathString.FromUriComponent(value) if you have a path value which is in an escaped format.
        /// </summary>
        /// <param name="value">The unescaped path to be assigned to the Value property.</param>
        public PathString(string value)
        {
            if (!string.IsNullOrEmpty(value) && value[0] != '/')
            {
                throw new ArgumentException("路径必须以“/”开头!", "value");
            }
            _value = value;
        }

        /// <summary>
        /// Provides the path string escaped in a way which is correct for combining into the URI representation.
        /// </summary>
        /// <returns>The escaped path value</returns>
        public override string ToString()
        {
            return ToUriComponent();
        }

        /// <summary>
        /// Provides the path string escaped in a way which is correct for combining into the URI representation.
        /// </summary>
        /// <returns>The escaped path value</returns>
        public string ToUriComponent()
        {
            if (!HasValue)
            {
                return string.Empty;
            }
            StringBuilder stringBuilder = null;
            int startIndex = 0;
            int num = 0;
            bool flag = false;
            int num2 = 0;
            while (num2 < _value.Length)
            {
                bool flag2 = PathStringHelper.IsPercentEncodedChar(_value, num2);
                if (PathStringHelper.IsValidPathChar(_value[num2]) | flag2)
                {
                    if (flag)
                    {
                        if (stringBuilder == null)
                        {
                            stringBuilder = new StringBuilder(_value.Length * 3);
                        }
                        stringBuilder.Append(Uri.EscapeDataString(_value.Substring(startIndex, num)));
                        flag = false;
                        startIndex = num2;
                        num = 0;
                    }
                    if (flag2)
                    {
                        num += 3;
                        num2 += 3;
                    }
                    else
                    {
                        num++;
                        num2++;
                    }
                    continue;
                }
                if (!flag)
                {
                    if (stringBuilder == null)
                    {
                        stringBuilder = new StringBuilder(_value.Length * 3);
                    }
                    stringBuilder.Append(_value, startIndex, num);
                    flag = true;
                    startIndex = num2;
                    num = 0;
                }
                num++;
                num2++;
            }
            if (num == _value.Length && !flag)
            {
                return _value;
            }
            if (num > 0)
            {
                if (stringBuilder == null)
                {
                    stringBuilder = new StringBuilder(_value.Length * 3);
                }
                if (flag)
                {
                    stringBuilder.Append(Uri.EscapeDataString(_value.Substring(startIndex, num)));
                }
                else
                {
                    stringBuilder.Append(_value, startIndex, num);
                }
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Returns an PathString given the path as it is escaped in the URI format. The string MUST NOT contain any
        /// value that is not a path.
        /// </summary>
        /// <param name="uriComponent">The escaped path as it appears in the URI format.</param>
        /// <returns>The resulting PathString</returns>
        public static PathString FromUriComponent(string uriComponent)
        {
            return new PathString(Uri.UnescapeDataString(uriComponent));
        }

        /// <summary>
        /// Returns an PathString given the path as from a Uri object. Relative Uri objects are not supported.
        /// </summary>
        /// <param name="uri">The Uri object</param>
        /// <returns>The resulting PathString</returns>
        public static PathString FromUriComponent(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }
            return new PathString("/" + uri.GetComponents(UriComponents.Path, UriFormat.Unescaped));
        }

        /// <summary>
        /// Determines whether the beginning of this <see cref="T:Microsoft.AspNetCore.Http.PathString" /> instance matches the specified <see cref="T:Microsoft.AspNetCore.Http.PathString" />.
        /// </summary>
        /// <param name="other">The <see cref="T:Microsoft.AspNetCore.Http.PathString" /> to compare.</param>
        /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
        public bool StartsWithSegments(PathString other)
        {
            return StartsWithSegments(other, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the beginning of this <see cref="T:Microsoft.AspNetCore.Http.PathString" /> instance matches the specified <see cref="T:Microsoft.AspNetCore.Http.PathString" /> when compared
        /// using the specified comparison option.
        /// </summary>
        /// <param name="other">The <see cref="T:Microsoft.AspNetCore.Http.PathString" /> to compare.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how this <see cref="T:Microsoft.AspNetCore.Http.PathString" /> and value are compared.</param>
        /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
        public bool StartsWithSegments(PathString other, StringComparison comparisonType)
        {
            string text = Value ?? string.Empty;
            string text2 = other.Value ?? string.Empty;
            if (text.StartsWith(text2, comparisonType))
            {
                if (text.Length != text2.Length)
                {
                    return text[text2.Length] == '/';
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the beginning of this <see cref="T:Microsoft.AspNetCore.Http.PathString" /> instance matches the specified <see cref="T:Microsoft.AspNetCore.Http.PathString" /> and returns
        /// the remaining segments.
        /// </summary>
        /// <param name="other">The <see cref="T:Microsoft.AspNetCore.Http.PathString" /> to compare.</param>
        /// <param name="remaining">The remaining segments after the match.</param>
        /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
        public bool StartsWithSegments(PathString other, out PathString remaining)
        {
            return StartsWithSegments(other, StringComparison.OrdinalIgnoreCase, out remaining);
        }

        /// <summary>
        /// Determines whether the beginning of this <see cref="T:Microsoft.AspNetCore.Http.PathString" /> instance matches the specified <see cref="T:Microsoft.AspNetCore.Http.PathString" /> when compared
        /// using the specified comparison option and returns the remaining segments.
        /// </summary>
        /// <param name="other">The <see cref="T:Microsoft.AspNetCore.Http.PathString" /> to compare.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how this <see cref="T:Microsoft.AspNetCore.Http.PathString" /> and value are compared.</param>
        /// <param name="remaining">The remaining segments after the match.</param>
        /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
        public bool StartsWithSegments(PathString other, StringComparison comparisonType, out PathString remaining)
        {
            string text = Value ?? string.Empty;
            string text2 = other.Value ?? string.Empty;
            if (text.StartsWith(text2, comparisonType) && (text.Length == text2.Length || text[text2.Length] == '/'))
            {
                remaining = new PathString(text.Substring(text2.Length));
                return true;
            }
            remaining = Empty;
            return false;
        }

        /// <summary>
        /// Determines whether the beginning of this <see cref="T:Microsoft.AspNetCore.Http.PathString" /> instance matches the specified <see cref="T:Microsoft.AspNetCore.Http.PathString" /> and returns
        /// the matched and remaining segments.
        /// </summary>
        /// <param name="other">The <see cref="T:Microsoft.AspNetCore.Http.PathString" /> to compare.</param>
        /// <param name="matched">The matched segments with the original casing in the source value.</param>
        /// <param name="remaining">The remaining segments after the match.</param>
        /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
        public bool StartsWithSegments(PathString other, out PathString matched, out PathString remaining)
        {
            return StartsWithSegments(other, StringComparison.OrdinalIgnoreCase, out matched, out remaining);
        }

        /// <summary>
        /// Determines whether the beginning of this <see cref="T:Microsoft.AspNetCore.Http.PathString" /> instance matches the specified <see cref="T:Microsoft.AspNetCore.Http.PathString" /> when compared
        /// using the specified comparison option and returns the matched and remaining segments.
        /// </summary>
        /// <param name="other">The <see cref="T:Microsoft.AspNetCore.Http.PathString" /> to compare.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how this <see cref="T:Microsoft.AspNetCore.Http.PathString" /> and value are compared.</param>
        /// <param name="matched">The matched segments with the original casing in the source value.</param>
        /// <param name="remaining">The remaining segments after the match.</param>
        /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
        public bool StartsWithSegments(PathString other, StringComparison comparisonType, out PathString matched, out PathString remaining)
        {
            string text = Value ?? string.Empty;
            string text2 = other.Value ?? string.Empty;
            if (text.StartsWith(text2, comparisonType) && (text.Length == text2.Length || text[text2.Length] == '/'))
            {
                matched = new PathString(text.Substring(0, text2.Length));
                remaining = new PathString(text.Substring(text2.Length));
                return true;
            }
            remaining = Empty;
            matched = Empty;
            return false;
        }

        /// <summary>
        /// Adds two PathString instances into a combined PathString value.
        /// </summary>
        /// <returns>The combined PathString value</returns>
        public PathString Add(PathString other)
        {
            if (HasValue && other.HasValue && Value[Value.Length - 1] == '/')
            {
                return new PathString(Value + other.Value.Substring(1));
            }
            return new PathString(Value + other.Value);
        }

        /// <summary>
        /// Compares this PathString value to another value. The default comparison is StringComparison.OrdinalIgnoreCase.
        /// </summary>
        /// <param name="other">The second PathString for comparison.</param>
        /// <returns>True if both PathString values are equal</returns>
        public bool Equals(PathString other)
        {
            return Equals(other, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compares this PathString value to another value using a specific StringComparison type
        /// </summary>
        /// <param name="other">The second PathString for comparison</param>
        /// <param name="comparisonType">The StringComparison type to use</param>
        /// <returns>True if both PathString values are equal</returns>
        public bool Equals(PathString other, StringComparison comparisonType)
        {
            if (!HasValue && !other.HasValue)
            {
                return true;
            }
            return string.Equals(_value, other._value, comparisonType);
        }

        /// <summary>
        /// Compares this PathString value to another value. The default comparison is StringComparison.OrdinalIgnoreCase.
        /// </summary>
        /// <param name="obj">The second PathString for comparison.</param>
        /// <returns>True if both PathString values are equal</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return !HasValue;
            }
            if (obj is PathString)
            {
                return Equals((PathString)obj);
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for the PathString value. The hash code is provided by the OrdinalIgnoreCase implementation.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            if (!HasValue)
            {
                return 0;
            }
            return StringComparer.OrdinalIgnoreCase.GetHashCode(_value);
        }

        /// <summary>
        /// Operator call through to Equals
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>True if both PathString values are equal</returns>
        public static bool operator ==(PathString left, PathString right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Operator call through to Equals
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>True if both PathString values are not equal</returns>
        public static bool operator !=(PathString left, PathString right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The ToString combination of both values</returns>
        public static string operator +(string left, PathString right)
        {
            return left + right.ToString();
        }

        /// <summary>
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The ToString combination of both values</returns>
        public static string operator +(PathString left, string right)
        {
            return left.ToString() + right;
        }

        /// <summary>
        /// Operator call through to Add
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The PathString combination of both values</returns>
        public static PathString operator +(PathString left, PathString right)
        {
            return left.Add(right);
        }

        /// <summary>
        /// Implicitly creates a new PathString from the given string.
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator PathString(string s)
        {
            return ConvertFromString(s);
        }

        /// <summary>
        /// Implicitly calls ToString().
        /// </summary>
        /// <param name="path"></param>
        public static implicit operator string(PathString path)
        {
            return path.ToString();
        }

        internal static PathString ConvertFromString(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                return FromUriComponent(s);
            }
            return new PathString(s);
        }
    }
}
#endif
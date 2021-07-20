using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace CodeArts.Serialize.Xml
{
    /// <summary>
    /// XML序列化帮助类。
    /// </summary>
    public class XmlHelper
    {
        private class MyXmlWriter : XmlWriter
        {
            private readonly XmlWriter writer;

            public MyXmlWriter(XmlWriter writer)
            {
                this.writer = writer;
            }

            public override WriteState WriteState => writer.WriteState;

            public override void Flush() => writer.Flush();

            public override string LookupPrefix(string ns) => writer.LookupPrefix(ns);

            public override void WriteBase64(byte[] buffer, int index, int count) => writer.WriteBase64(buffer, index, count);

            public override void WriteCData(string text)
            {
                throw new NotImplementedException();
            }

            public override void WriteCharEntity(char ch) => writer.WriteCharEntity(ch);

            public override void WriteChars(char[] buffer, int index, int count) => writer.WriteChars(buffer, index, count);

            public override void WriteComment(string text) => writer.WriteComment(text);

            public override void WriteDocType(string name, string pubid, string sysid, string subset) => writer.WriteDocType(name, pubid, sysid, subset);

            public override void WriteEndAttribute() => writer.WriteEndAttribute();

            public override void WriteEndDocument() => writer.WriteEndDocument();

            public override void WriteEndElement() => writer.WriteEndElement();

            public override void WriteEntityRef(string name) => writer.WriteEntityRef(name);

            public override void WriteFullEndElement() => writer.WriteFullEndElement();

            public override void WriteProcessingInstruction(string name, string text) => writer.WriteProcessingInstruction(name, text);

            public override void WriteRaw(char[] buffer, int index, int count) => writer.WriteRaw(buffer, index, count);

            public override void WriteRaw(string data) => writer.WriteRaw(data);

            public override void WriteStartAttribute(string prefix, string localName, string ns) => writer.WriteStartAttribute(prefix, localName, ns);

            public override void WriteStartDocument() => writer.WriteStartDocument();

            public override void WriteStartDocument(bool standalone) => writer.WriteStartDocument(standalone);

            public override void WriteStartElement(string prefix, string localName, string ns) => writer.WriteStartElement(prefix, localName, ns);

            public override void WriteString(string text) => writer.WriteString(text);

            public override void WriteSurrogateCharEntity(char lowChar, char highChar) => writer.WriteSurrogateCharEntity(lowChar, highChar);

            public override void WriteWhitespace(string ws) => writer.WriteWhitespace(ws);

            public override void Close() => writer.Close();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
#if NET40
                    ((IDisposable)writer).Dispose();
#else
                    writer.Dispose();
#endif
                }
            }

            public override void WriteAttributes(XmlReader reader, bool defattr)
            {
                writer.WriteAttributes(reader, defattr);
            }
            public override XmlWriterSettings Settings => writer.Settings;
            public override void WriteName(string name)
            {
                writer.WriteName(name);
            }

            public override void WriteNode(XPathNavigator navigator, bool defattr)
            {
                writer.WriteNode(navigator, defattr);
            }

            public override void WriteNode(XmlReader reader, bool defattr)
            {
                writer.WriteNode(reader, defattr);
            }

            public override void WriteNmToken(string name)
            {
                writer.WriteNmToken(name);
            }

            public override void WriteQualifiedName(string localName, string ns)
            {
                writer.WriteQualifiedName(localName, ns);
            }

            public override void WriteValue(bool value)
            {
                writer.WriteValue(value);
            }

            public override void WriteValue(DateTime value)
            {
                writer.WriteValue(value);
            }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            public override void WriteValue(DateTimeOffset value)
            {
                writer.WriteValue(value);
            }
#endif

            public override void WriteValue(decimal value)
            {
                writer.WriteValue(value);
            }

            public override void WriteValue(double value)
            {
                writer.WriteValue(value);
            }

            public override void WriteValue(float value)
            {
                writer.WriteValue(value);
            }
            public override void WriteValue(int value)
            {
                writer.WriteValue(value);
            }
            public override void WriteValue(long value)
            {
                writer.WriteValue(value);
            }
            public override void WriteValue(object value)
            {
                writer.WriteValue(value);
            }
            public override void WriteValue(string value)
            {
                writer.WriteValue(value);
            }
            public override string XmlLang => writer.XmlLang;
            public override XmlSpace XmlSpace => writer.XmlSpace;
        }

        /// <summary>
        /// XML序列化实现。
        /// </summary>
        /// <param name="stream">流。</param>
        /// <param name="obj">对象。</param>
        /// <param name="encoding">编码方式。</param>
        /// <param name="indented">是否缩进。</param>
        private static void XmlSerializeInternal(Stream stream, object obj, Encoding encoding, bool indented)
        {
            var serializer = new XmlSerializer(obj.GetType());

            var settings = new XmlWriterSettings
            {
                Indent = indented,
                NewLineChars = Environment.NewLine,
                Encoding = encoding
            };

            using (XmlWriter writer = new MyXmlWriter(XmlWriter.Create(stream, settings)))
            {
                serializer.Serialize(writer, obj);
                writer.Close();
            }
        }

        /// <summary>
        /// 将一个对象序列化为XML字符串。
        /// </summary>
        /// <param name="obj">要序列化的对象。</param>
        /// <param name="encoding">编码方式，默认：UTF8。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns>序列化产生的XML字符串。</returns>
        public static string XmlSerialize(object obj, Encoding encoding = null, bool indented = false)
        {
            if (obj is null) return null;

            if (encoding is null)
            {
                encoding = Encoding.UTF8;
            }

            using (var stream = new MemoryStream())
            {
                XmlSerializeInternal(stream, obj, encoding, indented);
                stream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(stream, encoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// 从XML字符串中反序列化对象。
        /// </summary>
        /// <typeparam name="T">结果对象类型。</typeparam>
        /// <param name="xml">包含对象的XML字符串。</param>
        /// <param name="encoding">编码方式，默认：UTF8。</param>
        /// <returns>反序列化得到的对象。</returns>
        public static T XmlDeserialize<T>(string xml, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(xml))
                return default;

            if (encoding is null)
            {
                encoding = Encoding.UTF8;
            }

            try
            {
                var mySerializer = new XmlSerializer(typeof(T));

                using (var ms = new MemoryStream(encoding.GetBytes(xml)))
                {
                    using (var stream = new StreamReader(ms, encoding))
                    {
                        return (T)mySerializer.Deserialize(stream);
                    }
                }
            }
            catch
            {
                return default;
            }
        }
    }
}

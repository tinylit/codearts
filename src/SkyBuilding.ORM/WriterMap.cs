using System;
using System.Collections.Generic;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// SQL写入流
    /// </summary>
    public class WriterMap : IWriterMap
    {
        private readonly ISQLCorrectSettings _settings;
        public WriterMap(ISQLCorrectSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
        public string Delimiter => ",";
        public string OpenBrace => "(";
        public string CloseBrace => ")";
        public string EmptyString => "''";
        public string WhiteSpace => " ";
        public virtual string Alias(string name) => _settings.AsName(name);

        public virtual string Paramter(string name) => _settings.ParamterName(name);

        public virtual string Name(string name) => _settings.Name(name);

        public virtual string TableName(string name) => _settings.TableName(name);
    }
}

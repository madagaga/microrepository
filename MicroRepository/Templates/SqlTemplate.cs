using MicroRepository.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Templates
{
    public class SqlTemplate
    {
        public string Select { get; set; }
        public string Update { get; set; }
        public string Insert { get; set; }
        public string Delete { get; set; }

        public string Identity { get; set; }
        public string Separator { get; set; }

        public string QuoteChar { get; set; }

        public string Enquote(string column)
        {
            return string.Format(QuoteChar, column);
        }

       internal static SqlTemplate Load(string dbType)
        {
            object template = Resources.ResourceManager.GetObject(dbType);
            SqlTemplate result = null;
            if (template == null)
                throw new System.IO.FileNotFoundException("No template file found");
            using (System.IO.MemoryStream reader = new System.IO.MemoryStream(template as byte[]))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(SqlTemplate));
                result = (SqlTemplate)serializer.Deserialize(reader);
                reader.Close();
            }
            return result;
        }

    }
}


using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MyFinanceModel.Utilities
{
    public class XmlConvertion
    {
        public static string SerializeToXml(object value)
        {
            if (value == null)
                return "";
            XmlSerializer xmlSerializer = new XmlSerializer(value.GetType());
            string resultString;
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, value);
                resultString = textWriter.ToString();
            }
            return resultString;
        }

        public static T DeserializeToXml<T>(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return default(T);
            var xdoc = XDocument.Parse(xml);
            return DeserializeToXml<T>(xdoc);
        }

        public static T DeserializeToXml<T>(XDocument doc)
        {
            if (doc == null || doc.Root == null)
                return default(T);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

            using (var reader = doc.Root.CreateReader())
            {
                return (T)xmlSerializer.Deserialize(reader);
            }
        }

    }
}

using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace FontGeneratorCLI
{
    /// <summary>
    /// Provides wrapper methods for XML serialization.
    /// </summary>
    internal static class XmlHelper
    {
        /// <summary>
        /// Serializes object into XML file.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="filename">Target filename.</param>
        /// <param name="obj">Object to serialize</param>
        /// <param name="nsPrefix">XML namespace prefix.</param>
        /// <param name="ns">XML namespace.</param>
        public static void Serialize<T>(string filename, T obj, string nsPrefix = "", string ns = "")
        {
            var nameSpace = new XmlSerializerNamespaces();
            nameSpace.Add(nsPrefix, ns);

            var serializer = new XmlSerializer(typeof(T));
            var settings = new XmlWriterSettings { Indent = true };

            using (XmlWriter writer = XmlWriter.Create(filename, settings))
            {
                serializer.Serialize(writer, obj, nameSpace);
            }
        }

        /// <summary>
        /// Deserializes object from XML file.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="filename">Filename from which object will be deserialized.</param>
        /// <param name="ns">XML namespace.</param>
        /// <returns>Deserialized object.</returns>
        public static T Deserialize<T>(string filename, string ns = "")
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T), ns);
            using (StreamReader file = new StreamReader(filename))
            {
                return (T)serializer.Deserialize(file);
            }
        }
    }
}

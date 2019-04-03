using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using NetInteractor.Core.Interacts;

namespace NetInteractor.Core.Config
{
    public static class ConfigFactory
    {
        public static IInteractActionConfig DeserializeActionConfig(XmlElement element)
        {
            switch (element.LocalName.ToLower())
            {
                case ("get"):
                    return DeserializeElement<GetConfig>(element);
                case ("post"):
                    return DeserializeElement<PostConfig>(element);
                case ("if"):
                    return DeserializeElement<IfConfig>(element);
                case ("call"):
                    return DeserializeElement<CallConfig>(element);
                default:
                    throw new Exception("Unknow action element: " + element.LocalName);
            }
        }

        public static TConfig DeserializeElement<TConfig>(XmlElement element)
            where TConfig : class
        {
            var serializer = new XmlSerializer(typeof(TConfig));
            serializer.UnknownElement += (s, e) =>
            {
                var x = e.ObjectBeingDeserialized as IUnknownElementHandler;
                x?.AppendUnknownElement(e.Element);
            };

            return (TConfig)serializer.Deserialize(new XmlNodeReader(element));
        }

        public static TConfig DeserializeXml<TConfig>(string xml)
            where TConfig : class
        {
            var serializer = new XmlSerializer(typeof(TConfig));

            serializer.UnknownElement += (s, e) =>
            {
                var x = e.ObjectBeingDeserialized as IUnknownElementHandler;
                x?.AppendUnknownElement(e.Element);
            };

            using (var reader = XmlReader.Create(new StringReader(xml)))
            {
                return (TConfig)serializer.Deserialize(reader);
            }
        }
    }
}
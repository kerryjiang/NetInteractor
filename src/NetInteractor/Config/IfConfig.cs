using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using NetInteractor.Interacts;

namespace NetInteractor.Config
{
    [Serializable]
    [XmlRoot("if")]
    public class IfConfig : IInteractActionConfig, IUnknownElementHandler
    {
        [XmlAttribute("property")]
        public string Property { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }

        [XmlIgnore]
        public IInteractActionConfig Child { get; private set; }

        public IInteractAction GetAction()
        {
            return new If(this);
        }

        void IUnknownElementHandler.AppendUnknownElement(XmlElement element)
        {
            Child = ConfigFactory.DeserializeActionConfig(element);
        }
    }
}
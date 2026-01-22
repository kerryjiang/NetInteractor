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

        private NameValueCollection _options;

        [XmlAnyAttribute]
        public XmlAttribute[] UnknownAttributes
        {
            get { return null; }
            set
            {
                if (value != null && value.Length > 0)
                {
                    _options = new NameValueCollection();
                    foreach (var attr in value)
                    {
                        _options.Add(attr.Name, attr.Value);
                    }
                }
            }
        }

        [XmlIgnore]
        public NameValueCollection Options
        {
            get { return _options ?? (_options = new NameValueCollection()); }
        }

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
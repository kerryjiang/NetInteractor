using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace NetInteractor.Config
{
    public interface IInteractActionConfig
    {
        IInteractAction GetAction();

        NameValueCollection Options { get; }
    }

    public abstract class InteractActionConfig : IInteractActionConfig
    {

        [XmlElement("output")]
        public OutputValueConfig[] Outputs { get; set; }

        [XmlAttribute("expectedHttpStatusCodes")]        
        public string ExpectedHttpStatusCodes { get; set; }

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

        public abstract IInteractAction GetAction();
    }
}
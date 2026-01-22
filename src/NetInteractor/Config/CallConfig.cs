using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using NetInteractor.Interacts;

namespace NetInteractor.Config
{
    [Serializable]
    [XmlRoot("call")]
    public class CallConfig : IInteractActionConfig
    {
        [XmlAttribute("target")]
        public string Target { get; set; }

        public IInteractAction GetAction()
        {
            return new Call(this);
        }
    }
}
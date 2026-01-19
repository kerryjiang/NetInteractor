using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NetInteractor.Core.Interacts;

namespace NetInteractor.Core.Config
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
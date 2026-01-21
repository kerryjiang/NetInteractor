using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NetInteractor.Interacts;

namespace NetInteractor.Config
{
    [Serializable]
    [XmlRoot("get")]
    public class GetConfig : InteractActionConfig
    {
        [XmlAttribute("url")]
        public string Url { get; set; }

        public override IInteractAction GetAction()
        {
            return new Get(this);
        }
    }
}
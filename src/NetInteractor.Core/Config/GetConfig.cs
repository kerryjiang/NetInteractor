using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NetInteractor.Core.Interacts;

namespace NetInteractor.Core.Config
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
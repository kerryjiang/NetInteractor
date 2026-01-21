using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NetInteractor.Interacts;

namespace NetInteractor.Config
{
    [Serializable]
    [XmlRoot("post")]
    public class PostConfig : InteractActionConfig
    {
        [XmlAttribute("formIndex")]
        public int FormIndex { get; set; }

        [XmlAttribute("formName")]
        public string FormName { get; set; }

        [XmlAttribute("action")]
        public string Action { get; set; }
        
        [XmlAttribute("clientID")]
        public string ClientID { get; set; }

        [XmlElement("formValue")]
        public FormValue[] FormValues { get; set; }

        public override IInteractAction GetAction()
        {
            return new Post(this);
        }
    }
}
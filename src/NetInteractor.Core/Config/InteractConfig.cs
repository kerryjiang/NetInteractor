using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NetInteractor.Core.Config
{
    public class InteractConfig
    {
        [XmlAttribute("defaultTarget")]
        public string DefaultTarget { get; set; }

        [XmlElement("target")]
        public TargetConfig[] Targets { get; set; }
    }
}
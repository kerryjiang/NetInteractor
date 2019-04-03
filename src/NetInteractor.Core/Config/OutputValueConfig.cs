using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NetInteractor.Core.Config
{
    public class OutputValueConfig
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        

        [XmlAttribute("regex")]
        public string Regex { get; set; }


        [XmlAttribute("xpath")]
        public string Xpath { get; set; }

        [XmlAttribute("attr")]
        public string Attr { get; set; }


        [XmlAttribute("isMultipleValue")]
        public bool IsMultipleValue { get; set; }

        [XmlAttribute("expectedValue")]
        public string ExpectedValue { get; set; }
    }
}
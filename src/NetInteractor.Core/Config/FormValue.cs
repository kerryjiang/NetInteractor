using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NetInteractor.Core.Config
{
    public class FormValue
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }

        [XmlAttribute("text")]
        public string Text { get; set; }
    }
}
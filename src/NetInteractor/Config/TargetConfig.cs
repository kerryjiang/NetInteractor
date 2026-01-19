using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace NetInteractor.Core.Config
{
    public class TargetConfig : IUnknownElementHandler
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        public IReadOnlyList<IInteractActionConfig> Actions
        {
            get { return actions; }
        }

        private List<IInteractActionConfig> actions;

        public TargetConfig()
        {
            actions = new List<IInteractActionConfig>();
        }

        void IUnknownElementHandler.AppendUnknownElement(XmlElement element)
        {
            actions.Add(ConfigFactory.DeserializeActionConfig(element));
        }
    }
}
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NetInteractor.Core.Config
{
    public interface IInteractActionConfig
    {
        IInteractAction GetAction();
    }

    public abstract class InteractActionConfig : IInteractActionConfig
    {

        [XmlElement("output")]
        public OutputValueConfig[] Outputs { get; set; }

        public abstract IInteractAction GetAction();
    }
}
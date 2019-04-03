using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml;
using NetInteractor.Core.Interacts;

namespace NetInteractor.Core.Config
{
    public interface IUnknownElementHandler
    {
        void AppendUnknownElement(XmlElement element);
    }
}
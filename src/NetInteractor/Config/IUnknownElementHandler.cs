using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml;
using NetInteractor.Interacts;

namespace NetInteractor.Config
{
    public interface IUnknownElementHandler
    {
        void AppendUnknownElement(XmlElement element);
    }
}
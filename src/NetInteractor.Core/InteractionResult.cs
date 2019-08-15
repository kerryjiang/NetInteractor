using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;

namespace NetInteractor.Core
{
    public class InteractionResult
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public string Target { get; set; }
        
        public NameValueCollection Outputs { get; set; }
    }
}
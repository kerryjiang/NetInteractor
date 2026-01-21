using System;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace NetInteractor
{
    public class InterationContext
    {
        public IWebAccessor WebAccessor { get; set; }

        public PageInfo CurrentPage { get; set; }

        public NameValueCollection Inputs { get; set; }

        public NameValueCollection Outputs { get; set; }

        internal int RedirectCount { get; set; }
    }
}
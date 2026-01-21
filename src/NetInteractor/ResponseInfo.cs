using System;
using System.Linq;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using System.Net.Http.Headers;

namespace NetInteractor
{
    public class ResponseInfo
    {
        public int StatusCode { get; set; }

        public string StatusDescription { get; set; }

        public string Url { get; set; }

        public string Html { get; set; }

        public HttpResponseHeaders Headers { get; set;  }
    }
}
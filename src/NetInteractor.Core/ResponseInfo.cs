using System;
using System.Linq;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetInteractor.Core
{
    public class ResponseInfo
    {
        public int StatusCode { get; set; } 

        public string Url { get; set; }

        public string Html { get; set; }
    }
}
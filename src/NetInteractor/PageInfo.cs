using System;
using System.Linq;
using System.Collections.Specialized;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetInteractor
{
    public class PageInfo
    {
        static PageInfo()
        {
            HtmlNode.ElementsFlags.Remove("form");
            HtmlNode.ElementsFlags.Remove("option");
        }

        public string Url { get; private set; }

        public string Html { get; private set; }

        public HtmlDocument Document { get; private set; }

        public FormInfo[] Forms { get; private set; }

        public PageInfo(string url, string html)
        {
            Url = url;
            Html = html;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            Document = doc;

            var forms = doc.DocumentNode.SelectNodes("//form");

            if (forms != null && forms.Count > 0)
            {
                Forms = forms.OfType<HtmlNode>()
                    .Select(n => new FormInfo(n))
                    .ToArray();
            }
        }
    }
}
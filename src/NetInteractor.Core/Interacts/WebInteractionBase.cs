using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using NetInteractor.Core.Config;
using HtmlAgilityPack;
using System.Net;

namespace NetInteractor.Core.Interacts
{
    public abstract class WebInteractionBase<TConfig> : InteractionBase<TConfig>
        where TConfig : InteractActionConfig
    {
        class RegexInfo
        {
            public Regex Regex { get; set; }

            public bool IsMultpleValue { get; set; }
        }

        class XpathInfo
        {
            public string Xpath { get; set; }

            public string Attr { get; set; }

            public bool IsMultpleValue { get; set; }
        }

        private List<KeyValuePair<string, RegexInfo>> regexes = new List<KeyValuePair<string, RegexInfo>>();

        private List<KeyValuePair<string, XpathInfo>> xpaths = new List<KeyValuePair<string, XpathInfo>>();

        private int[] expectedHttpStatusCodes;

        protected WebInteractionBase(TConfig config)
            : base(config)
        {
            if (!string.IsNullOrEmpty(config.ExpectedHttpStatusCodes))
            {
                expectedHttpStatusCodes = config.ExpectedHttpStatusCodes.Split(',')
                    .Select(i => int.Parse(i.Trim()))
                    .ToArray();
            }
            else
            {
                expectedHttpStatusCodes = new int[] { 200 };
            }

            if (Config.Outputs != null && Config.Outputs.Any())
            {
                foreach (var output in Config.Outputs)
                {
                    if (!string.IsNullOrEmpty(output.Regex))
                    {
                        regexes.Add(new KeyValuePair<string, RegexInfo>(output.Name, new RegexInfo
                        {
                            Regex = new Regex(output.Regex, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
                            IsMultpleValue = output.IsMultipleValue
                        }));
                    }

                    if (!string.IsNullOrEmpty(output.Xpath))
                    {
                        xpaths.Add(new KeyValuePair<string, XpathInfo>(output.Name, new XpathInfo
                        {
                            Xpath = output.Xpath,
                            IsMultpleValue = output.IsMultipleValue,
                            Attr = output.Attr
                        }));
                    }
                }
            }
        } 

        protected abstract Task<ResponseInfo> MakeRequest(InterationContext context);

        public override async Task<InteractionResult> ExecuteAsync(InterationContext context)
        {
            var response = default(ResponseInfo);

            try
            {
                response = await MakeRequest(context);

                if (!expectedHttpStatusCodes.Contains(response.StatusCode))
                {
                    return new InteractionResult
                    {
                        Ok = false,
                        Message = ((HttpStatusCode)response.StatusCode).ToString()
                    };
                }
            }
            catch (Exception e)
            {
                return new InteractionResult { Ok = false, Message = e.Message };
            }

            var pageInfo = new PageInfo(response.Url, response.Html);
            
            context.CurrentPage = pageInfo;
            context.Outputs = GetOutputValues(pageInfo);

            if (!ValidateOutput(context.Outputs, out string message))
            {
                return new InteractionResult { Ok = false, Message = message };
            }

            return new InteractionResult { Ok = true };
        }

        private NameValueCollection GetOutputValues(PageInfo page)
        {
            var values = new NameValueCollection();

            foreach (var regex in regexes)
            {
                var regexValue = regex.Value;

                if (regexValue.IsMultpleValue)
                {
                    var matches = regexValue.Regex.Matches(page.Html);

                    if (matches == null || matches.Count <= 0)
                        continue;

                    var matchValues = matches.OfType<Match>().Select(m => m.Groups[regex.Key])
                        .Where(g => g != null)
                        .Select(g => g.Value)
                        .ToArray();

                    if (matchValues.Any())
                    {
                        values.Add(regex.Key, string.Join(",", matchValues));
                    }
                }
                else
                {
                    var match = regexValue.Regex.Match(page.Html);

                    if (match == null || !match.Success)
                        continue;

                    var group = match.Groups[regex.Key];

                    if (group == null)
                        continue;

                    values.Add(regex.Key, group.Value);
                }
            }

            foreach (var xpath in xpaths)
            {
                var xpathValue = xpath.Value;

                if (xpathValue.IsMultpleValue)
                {
                    var nodes = page.Document.DocumentNode.SelectNodes(xpathValue.Xpath)?.ToArray();

                    if (nodes == null || !nodes.Any())
                        continue;

                    var selectedValues = new string[nodes.Length];

                    for (var i = 0; i < nodes.Length; i++)
                    {
                        var n =  nodes[i];
                        selectedValues[i] = GetXpathNodeValue(n, xpathValue);
                    }
                    
                    values.Add(xpath.Key, string.Join(",", selectedValues));
                }
                else
                {
                    var node = page.Document.DocumentNode.SelectSingleNode(xpathValue.Xpath);
                    values.Add(xpath.Key, GetXpathNodeValue(node, xpathValue));
                }           
            }

            return values;
        }

        private string GetXpathNodeValue(HtmlNode node, XpathInfo xpathValue)
        {
            if ("html()".Equals(xpathValue.Attr, StringComparison.OrdinalIgnoreCase))
            {
                return node.InnerHtml.Trim();
            }
            else if ("text()".Equals(xpathValue.Attr, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(xpathValue.Attr))
            {
                return node.InnerText.Trim();
            }
            else
            {
                return node.GetAttributeValue(xpathValue.Attr, string.Empty);
            }
        }

        private bool ValidateOutput(NameValueCollection outputValues, out string message)
        {
            if (Config.Outputs != null && Config.Outputs.Any())
            {
                foreach (var v in Config.Outputs.Where(o => !string.IsNullOrEmpty(o.ExpectedValue)))
                {
                    var actualValue = GetOutputValue(outputValues, v.Name);

                    if(!v.ExpectedValue.Equals(actualValue, StringComparison.OrdinalIgnoreCase))
                    {
                        message = $"Expected:{v.ExpectedValue}, but the actual value is: {actualValue}";
                        return false;
                    }
                }
            }
            
            message = string.Empty;
            return true;            
        }
    }
}
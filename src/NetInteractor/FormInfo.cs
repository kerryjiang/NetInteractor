using System;
using System.Linq;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetInteractor.Config;

namespace NetInteractor
{
    public class FormInfo
    {
        public string Name { get; private set; }

        public string ClientID { get; set; }

        public string Action { get; set; }

        public NameValueCollection FormValues { get; private set; }

        private HtmlNode node;

        public FormInfo(HtmlNode node)
        {
            Name = node.GetAttributeValue("name", string.Empty);
            ClientID = node.GetAttributeValue("id", string.Empty);
            Action = node.GetAttributeValue("action", string.Empty);
            this.node = node;
            FormValues = GetFormValues();
        }

        private NameValueCollection GetFormValues()
        {
            var formValues = new NameValueCollection();

            var formInputGroups = node.SelectNodes("//input")
                ?.OfType<HtmlNode>()
                ?.Select(n => new KeyValuePair<string, HtmlNode>(n.GetAttributeValue("name", string.Empty), n))
                .GroupBy(x => x.Key);

            if (formInputGroups != null)
            {
                foreach (var group in formInputGroups)
                {
                    var input = group.FirstOrDefault();
            
                    var nodeType = input.Value.GetAttributeValue("type", string.Empty);

                    if (nodeType.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
                    {
                        var selectedValues = group.Where(n =>
                            n.Value.GetAttributeValue("checked", bool.FalseString) == bool.TrueString)
                            .Select(n => n.Value.GetAttributeValue("value", string.Empty))
                            .ToArray();

                        formValues.Add(group.Key, string.Join(",", selectedValues));
                        continue;
                    }

                    if (nodeType.Equals("radio", StringComparison.OrdinalIgnoreCase))
                    {
                        var selectedValues = group.Where(n =>
                            n.Value.GetAttributeValue("checked", bool.FalseString) != bool.FalseString)
                            .Select(n => n.Value.GetAttributeValue("value", string.Empty))
                            .ToArray();

                        formValues.Add(group.Key, string.Join(",", selectedValues));
                        continue;
                    }

                    var selectedInputValues = group
                        .Select(n => n.Value.GetAttributeValue("value", string.Empty))
                        .ToArray();

                    formValues.Add(group.Key, string.Join(",", selectedInputValues));
                }
            }

            var selects = node.SelectNodes("//select")?.OfType<HtmlNode>();

            if (selects != null)
            {
                foreach (var select in selects)
                {
                    var selectedValue = select.SelectNodes("option").OfType<HtmlNode>()
                        .FirstOrDefault(n => n.GetAttributeValue("selected", bool.FalseString) != bool.FalseString)
                        ?.GetAttributeValue("value", string.Empty);

                    if (selectedValue == null)
                        continue;

                    formValues.Add(select.GetAttributeValue("name", string.Empty), selectedValue);
                }
            }

            return formValues;
        }

        public string GetSelectedValueByText(string fieldName, string text)
        {
            var select = node.SelectSingleNode($"//select[@name='{fieldName}']");

            if (select == null)
                throw new Exception($"the select with the name {fieldName} cannot be found.");

            var selectedValue = select.SelectNodes("option").OfType<HtmlNode>()
                .FirstOrDefault(n => text.Equals(n.InnerText.Trim(), StringComparison.OrdinalIgnoreCase))
                ?.GetAttributeValue("value", string.Empty);

            if (string.IsNullOrEmpty(selectedValue))
                throw new Exception($"the select {fieldName} doesn't have a option with the text '{text}'.");

            return selectedValue;
        }
    }
}
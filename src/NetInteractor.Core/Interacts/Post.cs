using System;
using System.Linq;
using System.Collections.Specialized;
using System.Threading.Tasks;
using NetInteractor.Core;
using NetInteractor.Core.Config;

namespace NetInteractor.Core.Interacts
{
    public class Post : WebInteractionBase<PostConfig>
    {
        public Post(PostConfig config)
            : base(config)
        {
            
        }

        private FormInfo GetForm(PageInfo page)
        {
            var config = Config;
            var form = default(FormInfo);

            if (!string.IsNullOrEmpty(config.ClientID))
            {
                form = page.Forms.FirstOrDefault(f =>
                    f.ClientID.Equals(config.ClientID, StringComparison.OrdinalIgnoreCase));
                
                if (form == null)
                {
                    throw new Exception("Cannot find a form by ClientID:" + config.ClientID);
                }
            }

            if (!string.IsNullOrEmpty(config.FormName))
            {
                form = page.Forms.FirstOrDefault(f =>
                    f.Name.Equals(config.FormName, StringComparison.OrdinalIgnoreCase));
                
                if (form == null)
                {
                    throw new Exception("Cannot find a form by FormName:" + config.FormName);
                }
            }

            if (!string.IsNullOrEmpty(config.Action))
            {
                form = page.Forms.FirstOrDefault(f =>
                    f.Action.Equals(config.Action, StringComparison.OrdinalIgnoreCase));
                
                if (form == null)
                {
                    throw new Exception("Cannot find a form by Action:" + config.Action);
                }
            }

            if (config.FormIndex >= 0)
            {
                if (page.Forms.Length <= config.FormIndex)
                {
                    throw new Exception("Form index is out of range:" + config.FormIndex);                    
                }

                form = page.Forms[config.FormIndex];
            }

            return form;
        }

        protected override async Task<ResponseInfo> MakeRequest(InterationContext context)
        {
            var config = Config;
            var page = context.CurrentPage;
            var form = GetForm(page);

            if (form == null)
                throw new Exception("No form ws found");

            var formValues = MergeFormValues(context, form, config.FormValues);
            var webAccessor = context.WebAccessor;

            var url = PrepareValue(context, page.Url);
            
            if (!string.IsNullOrEmpty(form.Action))
            {
                if (form.Action.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || form.Action.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    url = form.Action;
                }
                else if (form.Action.StartsWith("/"))
                {
                    var uri = new Uri(url);
                    url = uri.GetLeftPart(UriPartial.Authority) + form.Action;
                }
                else
                {
                    var pos = url.LastIndexOf("/");
                    url = url.Substring(0, pos + 1) + form.Action;
                }
            }

            return await webAccessor.PostAsync(url, formValues);
        }

        private NameValueCollection MergeFormValues(InterationContext context, FormInfo form, FormValue[] formValues)
        {
            var finalFormValues = form.FormValues;

            foreach (var v in formValues)
            {
                if (!string.IsNullOrEmpty(v.Value))
                    finalFormValues[v.Name] = this.PrepareValue(context, v.Value);
                else if (!string.IsNullOrEmpty(v.Text))
                    finalFormValues[v.Name] = form.GetSelectedValueByText(v.Name, this.PrepareValue(context, v.Text));
            }

            return finalFormValues;
        }
    }
}
using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;
using NetInteractor.Config;

namespace NetInteractor
{
    public interface IWebAccessor
    {
        Task<ResponseInfo> GetAsync(string url, InteractActionConfig config = null);

        Task<ResponseInfo> PostAsync(string url, NameValueCollection formValues, InteractActionConfig config = null);
    }
}

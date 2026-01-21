using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;

namespace NetInteractor
{
    public interface IWebAccessor
    {
        Task<ResponseInfo> GetAsync(string url);

        Task<ResponseInfo> PostAsync(string url, NameValueCollection formValues);
    }
}

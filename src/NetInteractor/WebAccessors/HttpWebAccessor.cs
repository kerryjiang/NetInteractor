using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;

namespace NetInteractor.Core.WebAccessors
{
    public class HttpWebAccessor : IWebAccessor
    {
        private readonly string _userAgent;
        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _httpClientHandler;

        public CookieContainer CookieContainer { get; set; }

        public HttpWebAccessor(string userAgent, CookieContainer cookieContainer)
        {
            _userAgent = userAgent;
            CookieContainer = cookieContainer;

            _httpClientHandler = new HttpClientHandler
            {
                CookieContainer = CookieContainer,
                UseCookies = true
            };

            _httpClient = new HttpClient(_httpClientHandler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
        }

        public HttpWebAccessor()
            : this("Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.62 Safari/537.36", new CookieContainer())
        {
        }

        public async Task<ResponseInfo> GetAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = await _httpClient.SendAsync(request);

            return await GetResultFromResponse(response);
        }

        public async Task<ResponseInfo> PostAsync(string url, NameValueCollection formValues)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            var formContent = string.Join("&", formValues.Keys.OfType<string>().Select(k =>
                    k + "=" + Uri.EscapeDataString(formValues[k])));

            request.Content = new StringContent(formContent, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.SendAsync(request);

            return await GetResultFromResponse(response);
        }

        private async Task<ResponseInfo> GetResultFromResponse(HttpResponseMessage response)
        {
            var html = await response.Content.ReadAsStringAsync();

            return new ResponseInfo
            {
                StatusCode = (int)response.StatusCode,
                Html = html,
                Url = response.RequestMessage?.RequestUri?.ToString()
            };
        }
    }
}

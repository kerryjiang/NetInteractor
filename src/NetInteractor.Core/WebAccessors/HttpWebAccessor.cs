using System;
using System.Collections.Specialized;
using System.Linq;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Text;

namespace NetInteractor.Core.WebAccessors
{
    public class HttpWebAccessor : IWebAccessor
    {
        private string userAgent;
        
        private CookieContainer cookieContainer;

        public HttpWebAccessor()
        {
            userAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.62 Safari/537.36";
            cookieContainer = new CookieContainer();
        }

        public async Task<ResponseInfo> GetAsync(string url)
        {
            var request = WebRequest.Create(url) as HttpWebRequest;

            request.CookieContainer = cookieContainer;
            request.UserAgent = userAgent;
            request.Method = "GET";

            var response = (await request.GetResponseAsync()) as HttpWebResponse;

            var html = string.Empty;
            
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                html = await reader.ReadToEndAsync();
            }

            return new ResponseInfo
            {
                StatusCode = (int)response.StatusCode,
                Html = html,
                Url = response.ResponseUri?.ToString()
            };
        }

        public async Task<ResponseInfo> PostAsync(string url, NameValueCollection formValues)
        {
            var request = WebRequest.Create(url) as HttpWebRequest;

            request.CookieContainer = cookieContainer;
            request.UserAgent = userAgent;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            var formData = Encoding.UTF8.GetBytes(string.Join("&", formValues.Keys.OfType<string>().Select(k =>
                    k + "=" + Uri.EscapeDataString(formValues[k]))
                    .ToArray()));

            request.ContentLength = formData.Length;

            var requestStream = await request.GetRequestStreamAsync();
            // Send the data.
            await requestStream.WriteAsync(formData, 0, formData.Length);
            await requestStream.FlushAsync();
            requestStream.Close();

            var response = (await request.GetResponseAsync()) as HttpWebResponse;

            var html = string.Empty;
            
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                html = await reader.ReadToEndAsync();
            }

            return new ResponseInfo
            {
                StatusCode = (int)response.StatusCode,
                Html = html,
                Url = response.ResponseUri?.ToString()
            };
        }
    }
}

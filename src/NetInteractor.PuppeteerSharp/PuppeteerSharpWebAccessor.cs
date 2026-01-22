using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace NetInteractor.WebAccessors
{
    public class PuppeteerSharpWebAccessor : IWebAccessor, IDisposable
    {
        private IBrowser _browser;
        private readonly LaunchOptions _launchOptions;
        private readonly SemaphoreSlim _browserLock = new SemaphoreSlim(1, 1);
        private bool _disposed;

        public PuppeteerSharpWebAccessor(LaunchOptions launchOptions = null)
        {
            _launchOptions = launchOptions ?? new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            };
        }

        private async Task<IBrowser> GetBrowserAsync()
        {
            if (_browser == null)
            {
                await _browserLock.WaitAsync();
                try
                {
                    // Double-check after acquiring the lock
                    if (_browser == null)
                    {
                        // Check if a custom executable path is specified via environment variable or launch options
                        var executablePath = _launchOptions.ExecutablePath ?? 
                                           Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
                        
                        LaunchOptions launchOptions;
                        
                        if (string.IsNullOrEmpty(executablePath))
                        {
                            // Download browser if needed
                            var browserFetcher = new BrowserFetcher();
                            
                            // Download the browser (it will skip if already downloaded)
                            await browserFetcher.DownloadAsync();
                            
                            launchOptions = _launchOptions;
                        }
                        else
                        {
                            // Create a new LaunchOptions with the custom executable path
                            // to avoid modifying the shared instance.
                            // Note: Only essential properties (Headless, Args) are copied.
                            // If additional properties are needed, they should be set via
                            // the constructor's launchOptions parameter.
                            launchOptions = new LaunchOptions
                            {
                                Headless = _launchOptions.Headless,
                                Args = _launchOptions.Args,
                                ExecutablePath = executablePath
                            };
                        }
                        
                        _browser = await Puppeteer.LaunchAsync(launchOptions);
                    }
                }
                finally
                {
                    _browserLock.Release();
                }
            }
            return _browser;
        }

        public async Task<ResponseInfo> GetAsync(string url)
        {
            var browser = await GetBrowserAsync();
            var page = await browser.NewPageAsync();

            try
            {
                var response = await page.GoToAsync(url, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
                });

                return await GetResultFromResponse(page, response);
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        public async Task<ResponseInfo> PostAsync(string url, NameValueCollection formValues)
        {
            var browser = await GetBrowserAsync();
            var page = await browser.NewPageAsync();

            try
            {
                // Navigate to the page first
                await page.GoToAsync(url, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
                });

                // Set up request interception to modify the request to POST
                await page.SetRequestInterceptionAsync(true);
                
                var formData = string.Join("&", formValues.Keys.OfType<string>()
                    .Select(k => k + "=" + Uri.EscapeDataString(formValues[k])));

                var responseTaskSource = new TaskCompletionSource<IResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                EventHandler<RequestEventArgs> requestHandler = null;
                EventHandler<ResponseCreatedEventArgs> responseHandler = null;

                try
                {
                    requestHandler = async (sender, e) =>
                    {
                        if (e.Request.Url == url)
                        {
                            await e.Request.ContinueAsync(new Payload
                            {
                                Method = HttpMethod.Post,
                                PostData = formData,
                                Headers = e.Request.Headers.Concat(new[]
                                {
                                    new System.Collections.Generic.KeyValuePair<string, string>(
                                        "Content-Type", "application/x-www-form-urlencoded")
                                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                            });
                        }
                        else
                        {
                            await e.Request.ContinueAsync();
                        }
                    };

                    responseHandler = (sender, e) =>
                    {
                        if (e.Response.Url == url)
                        {
                            responseTaskSource.TrySetResult(e.Response);
                        }
                    };

                    page.Request += requestHandler;
                    page.Response += responseHandler;

                    // Register cancellation
                    cts.Token.Register(() => responseTaskSource.TrySetCanceled());

                    // Navigate again to trigger the POST
                    await page.GoToAsync(url, new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
                    });

                    var response = await responseTaskSource.Task;
                    return await GetResultFromResponse(page, response);
                }
                finally
                {
                    // Clean up event handlers
                    if (requestHandler != null)
                        page.Request -= requestHandler;
                    if (responseHandler != null)
                        page.Response -= responseHandler;
                    cts.Dispose();
                }
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        private async Task<ResponseInfo> GetResultFromResponse(IPage page, IResponse response)
        {
            var html = await page.GetContentAsync();
            
            // Create a mock HttpResponseHeaders since we cannot instantiate it directly
            // We'll store headers in a custom wrapper that mimics HttpResponseHeaders
            var mockHeaders = CreateMockHeaders(response.Headers);

            return new ResponseInfo
            {
                StatusCode = (int)response.Status,
                StatusDescription = response.StatusText,
                Html = html,
                Url = response.Url,
                Headers = mockHeaders
            };
        }

        private HttpResponseHeaders CreateMockHeaders(System.Collections.Generic.IDictionary<string, string> headers)
        {
            // Since HttpResponseHeaders is sealed and cannot be instantiated directly,
            // we create it via HttpResponseMessage which is the standard way
            var mockResponse = new System.Net.Http.HttpResponseMessage();
            
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    try
                    {
                        mockResponse.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    catch (ArgumentException)
                    {
                        // Some headers might be content headers, skip them
                    }
                    catch (FormatException)
                    {
                        // Invalid header format, skip it
                    }
                }
            }
            
            return mockResponse.Headers;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _browser?.Dispose();
                    _browserLock?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}

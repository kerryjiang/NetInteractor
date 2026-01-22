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
                // Navigate to the URL and wait for network to be idle
                // This will handle the initial page load and any immediate redirects
                var response = await page.GoToAsync(url, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
                });

                // After the page loads, check if JavaScript might trigger a delayed redirect
                // We wait for a short period to see if a navigation event occurs
                // This handles cases like: setTimeout(() => window.location.href = '/other', 500)
                await Task.Delay(100); // Small initial delay to let any immediate JS execute
                
                try
                {
                    // Try to wait for a navigation with a timeout
                    // If JavaScript triggers a redirect, we'll catch it here
                    response = await page.WaitForNavigationAsync(new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                        Timeout = 1500 // Wait up to 1.5 seconds for JS redirect
                    });
                }
                catch (PuppeteerException)
                {
                    // No additional navigation occurred - this is normal for non-redirecting pages
                    // Use the original response from GoToAsync
                }

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
                // Set up request interception to modify the initial request to POST
                await page.SetRequestInterceptionAsync(true);
                
                var formData = string.Join("&", formValues.Keys.OfType<string>()
                    .Select(k => k + "=" + Uri.EscapeDataString(formValues[k] ?? string.Empty)));

                var hasIntercepted = false;
                
                EventHandler<RequestEventArgs> requestHandler = null;

                try
                {
                    requestHandler = async (sender, e) =>
                    {
                        // Only intercept the first request to the target URL
                        if (!hasIntercepted && e.Request.Url == url)
                        {
                            hasIntercepted = true;
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
                            // Allow all other requests (including redirects) to proceed normally
                            await e.Request.ContinueAsync();
                        }
                    };

                    page.Request += requestHandler;

                    // Navigate to the URL - this will be intercepted and turned into a POST
                    // The navigation will handle any redirects automatically
                    var response = await page.GoToAsync(url, new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
                    });

                    return await GetResultFromResponse(page, response);
                }
                finally
                {
                    // Clean up event handlers
                    if (requestHandler != null)
                        page.Request -= requestHandler;
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

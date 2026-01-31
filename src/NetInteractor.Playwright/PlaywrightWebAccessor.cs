using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Playwright;
using NetInteractor.Config;

namespace NetInteractor.WebAccessors
{
    public class PlaywrightWebAccessor : IWebAccessor, IAsyncDisposable
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private readonly SemaphoreSlim _browserLock = new SemaphoreSlim(1, 1);
        private readonly BrowserTypeLaunchOptions _launchOptions;
        private bool _disposed;

        public PlaywrightWebAccessor(BrowserTypeLaunchOptions launchOptions = null)
        {
            _launchOptions = launchOptions ?? new BrowserTypeLaunchOptions
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
                    if (_browser == null)
                    {
                        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                        _browser = await _playwright.Chromium.LaunchAsync(_launchOptions);
                    }
                }
                finally
                {
                    _browserLock.Release();
                }
            }
            return _browser;
        }

        public async Task<ResponseInfo> GetAsync(string url, InteractActionConfig config = null)
        {
            var browser = await GetBrowserAsync();
            var page = await browser.NewPageAsync();

            try
            {
                var response = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });

                var loadDelayStr = config?.Options?.FirstOrDefault(attr => attr.Name == "loadDelay")?.Value;
                if (!string.IsNullOrEmpty(loadDelayStr) && int.TryParse(loadDelayStr, out var loadDelay))
                {
                    var delayTask = page.WaitForTimeoutAsync(loadDelay);
                    var navigationTask = page.WaitForNavigationAsync(new PageWaitForNavigationOptions
                    {
                        WaitUntil = WaitUntilState.NetworkIdle
                    });

                    var completedTask = await Task.WhenAny(delayTask, navigationTask);
                    if (completedTask == navigationTask)
                    {
                        try
                        {
                            response = await navigationTask;
                        }
                        catch (PlaywrightException)
                        {
                        }
                    }
                }

                return await GetResultFromResponse(page, response);
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        public async Task<ResponseInfo> PostAsync(string url, NameValueCollection formValues, InteractActionConfig config = null)
        {
            var browser = await GetBrowserAsync();
            var page = await browser.NewPageAsync();

            try
            {
                var formData = string.Join("&", formValues.Keys.OfType<string>()
                    .Select(k => k + "=" + Uri.EscapeDataString(formValues[k] ?? string.Empty)));

                await page.RouteAsync("**/*", async route =>
                {
                    var request = route.Request;
                    if (request.Url == url && request.Method == "GET")
                    {
                        await route.ContinueAsync(new RouteContinueOptions
                        {
                            Method = "POST",
                            PostData = Encoding.UTF8.GetBytes(formData),
                            Headers = request.Headers.Concat(new[]
                            {
                                new KeyValuePair<string, string>("Content-Type", "application/x-www-form-urlencoded")
                            }).ToDictionary(x => x.Key, x => x.Value)
                        });
                    }
                    else
                    {
                        await route.ContinueAsync();
                    }
                });

                var response = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });

                return await GetResultFromResponse(page, response);
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        private async Task<ResponseInfo> GetResultFromResponse(IPage page, IResponse response)
        {
            var html = await page.ContentAsync();

            var mockResponse = new HttpResponseMessage();
            IEnumerable<KeyValuePair<string, string>> headerPairs = response?.Headers ?? Enumerable.Empty<KeyValuePair<string, string>>();

            foreach (var header in headerPairs)
            {
                try
                {
                    mockResponse.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                catch (Exception)
                {
                }
            }

            return new ResponseInfo
            {
                StatusCode = (int)(response?.Status ?? 0),
                StatusDescription = response?.StatusText,
                Html = html,
                Url = response?.Url,
                Headers = mockResponse.Headers
            };
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_browser != null)
                {
                    await _browser.DisposeAsync();
                }
                _playwright?.Dispose();
                _browserLock.Dispose();
            }
        }
    }
}

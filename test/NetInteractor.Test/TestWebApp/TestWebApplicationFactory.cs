using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace NetInteractor.Test.TestWebApp
{
    public enum ServerMode
    {
        TestServer,  // In-memory TestServer for HttpClient
        Kestrel      // Real HTTP server for PuppeteerSharp
    }

    public class TestWebApplicationFactory : IDisposable
    {
        private readonly WebApplication _app;
        private readonly HttpClient _httpClient;
        private readonly TestServer _testServer;
        private static readonly string PagesDirectory;
        private readonly string _serverUrl;
        private readonly ServerMode _mode;

        static TestWebApplicationFactory()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            PagesDirectory = Path.Combine(assemblyLocation!, "TestWebApp", "Pages");
        }

        private static string LoadPage(string pageName)
        {
            return File.ReadAllText(Path.Combine(PagesDirectory, pageName));
        }

        public TestWebApplicationFactory(ServerMode mode = ServerMode.TestServer)
        {
            _mode = mode;
            var builder = WebApplication.CreateBuilder();
            
            if (mode == ServerMode.Kestrel)
            {
                // Configure to use Kestrel for real HTTP (required for PuppeteerSharp)
                builder.WebHost.UseKestrel(options =>
                {
                    // Use a dynamic port for Kestrel to avoid conflicts
                    // Listen on IPv4 loopback with dynamic port
                    options.Listen(System.Net.IPAddress.Loopback, 0);
                });
            }
            else
            {
                // Use TestServer for in-memory testing (faster for HttpClient)
                builder.WebHost.UseTestServer();
            }
            
            // Add services
            builder.Services.AddRouting();

            _app = builder.Build();

            // Home page
            _app.MapGet("/", async context =>
            {
                await context.Response.WriteAsync(LoadPage("home.html"));
            });

            // Products page
            _app.MapGet("/products", async context =>
            {
                await context.Response.WriteAsync(LoadPage("products.html"));
            });

            // Add to cart
            _app.MapPost("/cart/add", async context =>
            {
                var form = await context.Request.ReadFormAsync();
                var productId = form["productId"];
                var size = form["size"];
                var quantity = form["quantity"];

                // Store in session/cookie simulation
                context.Response.Cookies.Append("cart_item", $"{productId}:{size}:{quantity}");

                context.Response.Redirect("/cart");
            });

            // Cart page (has dynamic content)
            _app.MapGet("/cart", async context =>
            {
                var cartItem = context.Request.Cookies["cart_item"] ?? "1:Medium:1";
                var parts = cartItem.Split(':');

                var html = LoadPage("cart.html")
                    .Replace("{item_size}", parts[1])
                    .Replace("{item_quantity}", parts[2]);

                await context.Response.WriteAsync(html);
            });

            // Checkout page
            _app.MapGet("/checkout", async context =>
            {
                await context.Response.WriteAsync(LoadPage("checkout.html"));
            });

            // Submit checkout (has dynamic content)
            _app.MapPost("/checkout/submit", async context =>
            {
                var form = await context.Request.ReadFormAsync();
                var name = form["billing_name"];
                var email = form["email"];

                var html = LoadPage("order-confirmation.html")
                    .Replace("{customer_name}", name!)
                    .Replace("{customer_email}", email!);

                await context.Response.WriteAsync(html);
            });

            // Login page
            _app.MapGet("/login", async context =>
            {
                await context.Response.WriteAsync(LoadPage("login.html"));
            });

            // Login post
            _app.MapPost("/login", async context =>
            {
                var form = await context.Request.ReadFormAsync();
                var username = form["username"];
                var password = form["password"];

                if (username == "testuser" && password == "testpass")
                {
                    context.Response.Cookies.Append("auth", "authenticated");
                    context.Response.Redirect("/dashboard");
                }
                else
                {
                    await context.Response.WriteAsync(LoadPage("login-failed.html"));
                }
            });

            // Dashboard (protected)
            _app.MapGet("/dashboard", async context =>
            {
                var auth = context.Request.Cookies["auth"];

                if (auth != "authenticated")
                {
                    context.Response.Redirect("/login");
                    return;
                }

                await context.Response.WriteAsync(LoadPage("dashboard.html"));
            });

            // Simple data extraction test page
            _app.MapGet("/data", async context =>
            {
                await context.Response.WriteAsync(LoadPage("data.html"));
            });

            // 301 Redirect test endpoint
            _app.MapGet("/redirect-test", context =>
            {
                context.Response.Redirect("/products", permanent: true);
                return Task.CompletedTask;
            });

            // POST redirect test endpoint - redirects to order confirmation after POST
            _app.MapPost("/post-redirect-test", async context =>
            {
                var form = await context.Request.ReadFormAsync();
                // Pass data via query string in redirect URL
                var name = Uri.EscapeDataString(form["billing_name"].ToString());
                context.Response.Redirect($"/post-redirect-result?name={name}");
            });

            // Form page for POST redirect test
            _app.MapGet("/post-redirect-test-form", async context =>
            {
                await context.Response.WriteAsync(LoadPage("post-redirect-form.html"));
            });

            // Result page after POST redirect
            _app.MapGet("/post-redirect-result", async context =>
            {
                var name = context.Request.Query["name"].ToString();
                if (string.IsNullOrEmpty(name)) name = "Unknown";
                var html = LoadPage("post-redirect-result.html").Replace("{{name}}", name);
                await context.Response.WriteAsync(html);
            });

            _app.Start();
            
            if (mode == ServerMode.Kestrel)
            {
                // Get the actual URL assigned by Kestrel
                var addresses = _app.Urls;
                if (addresses.Count > 0)
                {
                    _serverUrl = addresses.First();
                }
                else
                {
                    _serverUrl = "http://localhost:5000"; // Fallback
                }
                
                // Create an HttpClient without BaseAddress so it works with absolute URLs
                _httpClient = new HttpClient();
            }
            else
            {
                // For TestServer, get the TestServer instance and create client
                _testServer = _app.GetTestServer();
                _httpClient = _testServer.CreateClient();
                _serverUrl = "http://localhost"; // TestServer doesn't have real URL, use placeholder
            }
        }

        public HttpClient CreateClient()
        {
            return _httpClient;
        }

        /// <summary>
        /// Gets the base URL of the server.
        /// For TestServer: returns "http://localhost" (placeholder, works with relative URLs)
        /// For Kestrel: returns actual server URL like "http://127.0.0.1:12345"
        /// </summary>
        public string ServerUrl => _serverUrl;

        public void Dispose()
        {
            _httpClient?.Dispose();
            _testServer?.Dispose();
            _app?.DisposeAsync().AsTask().Wait();
        }
    }
}

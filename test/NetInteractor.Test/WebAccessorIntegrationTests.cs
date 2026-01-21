using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using NetInteractor.Config;
using NetInteractor.WebAccessors;
using NetInteractor.Test.TestWebApp;
using Xunit;

namespace NetInteractor.Test
{
    /// <summary>
    /// Integration tests that run with multiple IWebAccessor implementations.
    /// This class reuses test logic to verify that different web accessors work correctly.
    /// </summary>
    public class WebAccessorIntegrationTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private static readonly string ScriptsDirectory;

        static WebAccessorIntegrationTests()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ScriptsDirectory = Path.Combine(assemblyLocation!, "Scripts");
        }

        private static InteractConfig LoadConfig(string configName)
        {
            var configPath = Path.Combine(ScriptsDirectory, configName);
            var xml = File.ReadAllText(configPath);
            return ConfigFactory.DeserializeXml<InteractConfig>(xml);
        }

        public WebAccessorIntegrationTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Creates web accessor instances for testing.
        /// Returns different IWebAccessor implementations to test with.
        /// </summary>
        public static IEnumerable<object[]> GetWebAccessors()
        {
            yield return new object[] { "HttpClient" };
            
            // PuppeteerSharp tests require downloading Chromium browser on first run
            // Uncomment the line below for local testing with PuppeteerSharp
            // yield return new object[] { "PuppeteerSharp" };
        }

        /// <summary>
        /// Creates the appropriate web accessor based on the type name.
        /// </summary>
        private IWebAccessor CreateWebAccessor(string accessorType)
        {
            switch (accessorType)
            {
                case "HttpClient":
                    var client = _factory.CreateClient();
                    return new HttpClientWebAccessor(client);
                
                case "PuppeteerSharp":
                    // PuppeteerSharp uses a real browser and makes real HTTP requests
                    // It can directly access the test server via its real HTTP URL
                    return new PuppeteerSharpWebAccessor();
                
                default:
                    throw new ArgumentException($"Unknown accessor type: {accessorType}");
            }
        }

        /// <summary>
        /// Gets the base URL to use in config files based on the accessor type.
        /// Both HttpClient and PuppeteerSharp now use the real HTTP server.
        /// </summary>
        private string GetBaseUrl(string accessorType)
        {
            return _factory.ServerUrl;
        }

        /// <summary>
        /// Loads and updates config with the appropriate base URL for the accessor type.
        /// </summary>
        private InteractConfig LoadConfigForAccessor(string configName, string accessorType)
        {
            var config = LoadConfig(configName);
            var baseUrl = GetBaseUrl(accessorType);
            
            // Update URLs in config to use the real server URL
            if (!string.IsNullOrEmpty(baseUrl))
            {
                UpdateConfigUrls(config, baseUrl);
            }
            
            return config;
        }

        /// <summary>
        /// Updates all URLs in the config to use the provided base URL.
        /// </summary>
        private void UpdateConfigUrls(InteractConfig config, string baseUrl)
        {
            if (config.Targets == null) return;
            
            foreach (var target in config.Targets)
            {
                if (target.Actions == null) continue;
                
                foreach (var action in target.Actions)
                {
                    // Check if this is a GetConfig with a URL
                    if (action is GetConfig getConfig && !string.IsNullOrEmpty(getConfig.Url))
                    {
                        getConfig.Url = ReplaceLocalhost(getConfig.Url, baseUrl);
                    }
                    // Check if this is a PostConfig with an Action (URL)
                    else if (action is PostConfig postConfig && !string.IsNullOrEmpty(postConfig.Action))
                    {
                        postConfig.Action = ReplaceLocalhost(postConfig.Action, baseUrl);
                    }
                }
            }
        }

        /// <summary>
        /// Replaces localhost URLs with the actual server base URL.
        /// </summary>
        private string ReplaceLocalhost(string url, string baseUrl)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(baseUrl))
                return url;
            
            // Handle absolute localhost URLs like http://localhost/ or http://localhost/path
            if (url.StartsWith("http://localhost/") || url == "http://localhost")
            {
                var path = url.Substring("http://localhost".Length);
                return baseUrl.TrimEnd('/') + path;
            }
            
            // Handle relative URLs
            if (url.StartsWith("/"))
            {
                return baseUrl.TrimEnd('/') + url;
            }
            
            return url;
        }

        [Theory]
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestGetRequest_ExtractTitle(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfigForAccessor("GetRequest_ExtractTitle.config", accessorType);

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Welcome to Test Shop", result.Outputs["title"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestGetRequest_ExtractMultipleValues(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfigForAccessor("GetRequest_ExtractMultipleValues.config", accessorType);

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Contains("Value One", result.Outputs["values"]);
            Assert.Contains("Value Two", result.Outputs["values"]);
            Assert.Contains("Value Three", result.Outputs["values"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestGetRequest_ExtractAttribute(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfigForAccessor("GetRequest_ExtractAttribute.config", accessorType);

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("/images/test.png", result.Outputs["imageSrc"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestGetRequest_ExtractWithRegex(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfigForAccessor("GetRequest_ExtractWithRegex.config", accessorType);

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("98765", result.Outputs["orderId"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestPostRequest_FormSubmission(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfigForAccessor("PostRequest_FormSubmission.config", accessorType);

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestPostRequest_FormSubmissionWithOutputExtraction(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfigForAccessor("PostRequest_FormSubmissionWithOutputExtraction.config", accessorType);

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Jane Smith", result.Outputs["customerName"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestMultiStepWorkflow_ShoppingCart(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfigForAccessor("MultiStepWorkflow_ShoppingCart.config", accessorType);

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("$25.00", result.Outputs["cartTotal"]);
            Assert.Equal("Medium", result.Outputs["itemSize"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }
    }
}

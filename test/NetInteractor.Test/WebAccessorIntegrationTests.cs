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
            yield return new object[] { "HttpClient", null }; // Will be created in test with factory client
            
            // Note: PuppeteerSharp tests are skipped in CI/automated environments
            // because they require downloading Chromium browser on first run
            // Uncomment the line below for local testing with PuppeteerSharp
            // yield return new object[] { "PuppeteerSharp", null };
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
                    // For PuppeteerSharp, we need to configure it to work with the test server
                    // Since TestServer only provides HttpClient access, we'll need a different approach
                    // For now, skip PuppeteerSharp in parameterized tests
                    throw new NotSupportedException("PuppeteerSharp requires a real HTTP endpoint");
                
                default:
                    throw new ArgumentException($"Unknown accessor type: {accessorType}");
            }
        }

        [Theory]
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestGetRequest_ExtractTitle(string accessorType, object _)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractTitle.config");

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
        public async Task TestGetRequest_ExtractMultipleValues(string accessorType, object _)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractMultipleValues.config");

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
        public async Task TestGetRequest_ExtractAttribute(string accessorType, object _)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractAttribute.config");

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
        public async Task TestGetRequest_ExtractWithRegex(string accessorType, object _)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractWithRegex.config");

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
        public async Task TestPostRequest_FormSubmission(string accessorType, object _)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("PostRequest_FormSubmission.config");

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
        public async Task TestPostRequest_FormSubmissionWithOutputExtraction(string accessorType, object _)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("PostRequest_FormSubmissionWithOutputExtraction.config");

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
        public async Task TestMultiStepWorkflow_ShoppingCart(string accessorType, object _)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("MultiStepWorkflow_ShoppingCart.config");

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

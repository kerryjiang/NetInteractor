using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NetInteractor.Config;
using NetInteractor.WebAccessors;
using NetInteractor.Test.TestWebApp;
using Xunit;

namespace NetInteractor.Test
{
    /// <summary>
    /// Provides web accessor test data for integration tests.
    /// </summary>
    public class WebAccessorTestData : IEnumerable<object[]>
    {
        private readonly TestWebApplicationFactory _factory;

        public WebAccessorTestData()
        {
            _factory = new TestWebApplicationFactory();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            // HttpClient accessor
            var httpClient = _factory.CreateClient();
            yield return new object[] { new HttpClientWebAccessor(httpClient), _factory.ServerUrl };
            
            // PuppeteerSharp accessor - disabled by default in CI/CD
            // Requires Chrome download which needs internet access not available in GitHub Actions
            // To enable locally: set environment variable ENABLE_PUPPETEER_TESTS=true
            if (Environment.GetEnvironmentVariable("ENABLE_PUPPETEER_TESTS") == "true")
            {
                yield return new object[] { new PuppeteerSharpWebAccessor(), _factory.ServerUrl };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class IntegrationTests
    {
        private static readonly string ScriptsDirectory;

        static IntegrationTests()
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

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestGetRequest_ExtractTitle(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractTitle.config");
            var inputs = new NameValueCollection { ["BaseUrl"] = baseUrl };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Welcome to Test Shop", result.Outputs["title"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestGetRequest_ExtractMultipleValues(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractMultipleValues.config");
            var inputs = new NameValueCollection { ["BaseUrl"] = baseUrl };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

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
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestGetRequest_ExtractAttribute(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractAttribute.config");
            var inputs = new NameValueCollection { ["BaseUrl"] = baseUrl };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("/images/test.png", result.Outputs["imageSrc"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestGetRequest_ExtractWithRegex(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractWithRegex.config");
            var inputs = new NameValueCollection { ["BaseUrl"] = baseUrl };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("98765", result.Outputs["orderId"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestGetRequest_ExpectedValueValidation_Success(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExpectedValueValidation_Success.config");
            var inputs = new NameValueCollection { ["BaseUrl"] = baseUrl };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestGetRequest_ExpectedValueValidation_Failure(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExpectedValueValidation_Failure.config");
            var inputs = new NameValueCollection { ["BaseUrl"] = baseUrl };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.False(result.Ok);
            Assert.Contains("Expected", result.Message);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestPostRequest_FormSubmission(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("PostRequest_FormSubmission.config");
            var inputs = new NameValueCollection { ["BaseUrl"] = baseUrl };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestPostRequest_FormSubmissionWithOutputExtraction(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("PostRequest_FormSubmissionWithOutputExtraction.config");
            var inputs = new NameValueCollection { ["BaseUrl"] = baseUrl };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Jane Smith", result.Outputs["customerName"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestLoginFlow_WithInputParameters(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("LoginFlow_WithInputParameters.config");

            var inputs = new NameValueCollection
            {
                ["BaseUrl"] = baseUrl,
                ["BillingName"] = "Test User",
                ["Email"] = "test@example.com"
            };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Test User", result.Outputs["customerName"]);
            Assert.Equal("test@example.com", result.Outputs["customerEmail"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestMultiStepWorkflow_ShoppingCart(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("MultiStepWorkflow_ShoppingCart.config");
            var inputs = new NameValueCollection { ["BaseUrl"] = baseUrl };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("$25.00", result.Outputs["cartTotal"]);
            Assert.Equal("Medium", result.Outputs["itemSize"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestCallTarget_ReusableWorkflow(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("CallTarget_ReusableWorkflow.config");
            var inputs = new NameValueCollection { ["BaseUrl"] = baseUrl };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Data Extraction Test", result.Outputs["title"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestConditionalExecution_IfStatement(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("ConditionalExecution_IfStatement.config");

            var inputs = new NameValueCollection
            {
                ["BaseUrl"] = baseUrl,
                ["ShouldLogin"] = "false"
            };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Data Extraction Test", result.Outputs["title"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestExecuteSpecificTarget(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("ExecuteSpecificTarget.config");
            var inputs = new NameValueCollection { ["BaseUrl"] = baseUrl };

            // Act - Execute specific target instead of default
            var result = await executor.ExecuteAsync(config, inputs, "Products");

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Test Product", result.Outputs["productName"]);
            Assert.Null(result.Outputs["title"]); // Should not have executed Main target
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestRedirect_301_FollowsRedirect(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("RedirectTest.config");
            var inputs = new NameValueCollection { ["BaseUrl"] = baseUrl };

            // Act - Should follow 301 redirect from /redirect-test to /products
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Products", result.Outputs["title"]); // Should get products page after redirect
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [ClassData(typeof(WebAccessorTestData))]
        public async Task TestRedirect_AfterPost_FollowsRedirect(IWebAccessor webAccessor, string baseUrl)
        {
            // Arrange
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("RedirectAfterPostTest.config");
            var inputs = new NameValueCollection { ["BaseUrl"] = baseUrl };

            // Act - Should follow redirect after POST from /post-redirect-test to /post-redirect-result
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Post Redirect Success", result.Outputs["title"]);
            Assert.Equal("Redirect User", result.Outputs["customerName"]);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }
    }
}

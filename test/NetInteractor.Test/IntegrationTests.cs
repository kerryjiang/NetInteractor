using System;
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
    public class IntegrationTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
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

        public IntegrationTests(TestWebApplicationFactory factory)
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
        /// Creates input parameters with the BaseUrl for config file parameter substitution.
        /// </summary>
        private NameValueCollection CreateInputs(NameValueCollection additionalInputs = null)
        {
            var inputs = new NameValueCollection
            {
                ["BaseUrl"] = _factory.ServerUrl
            };

            if (additionalInputs != null)
            {
                foreach (string key in additionalInputs.Keys)
                {
                    inputs[key] = additionalInputs[key];
                }
            }

            return inputs;
        }

        [Theory]
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestGetRequest_ExtractTitle(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractTitle.config");
            var inputs = CreateInputs();

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
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestGetRequest_ExtractMultipleValues(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractMultipleValues.config");
            var inputs = CreateInputs();

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
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestGetRequest_ExtractAttribute(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractAttribute.config");
            var inputs = CreateInputs();

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
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestGetRequest_ExtractWithRegex(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractWithRegex.config");
            var inputs = CreateInputs();

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
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestGetRequest_ExpectedValueValidation_Success(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExpectedValueValidation_Success.config");
            var inputs = CreateInputs();

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            
            // Cleanup
            if (webAccessor is IDisposable disposable)
                disposable.Dispose();
        }

        [Theory]
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestGetRequest_ExpectedValueValidation_Failure(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExpectedValueValidation_Failure.config");
            var inputs = CreateInputs();

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
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestPostRequest_FormSubmission(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("PostRequest_FormSubmission.config");
            var inputs = CreateInputs();

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

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
            var config = LoadConfig("PostRequest_FormSubmissionWithOutputExtraction.config");
            var inputs = CreateInputs();

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
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestLoginFlow_WithInputParameters(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("LoginFlow_WithInputParameters.config");

            var inputs = CreateInputs(new NameValueCollection
            {
                ["BillingName"] = "Test User",
                ["Email"] = "test@example.com"
            });

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
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestMultiStepWorkflow_ShoppingCart(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("MultiStepWorkflow_ShoppingCart.config");
            var inputs = CreateInputs();

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
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestCallTarget_ReusableWorkflow(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("CallTarget_ReusableWorkflow.config");
            var inputs = CreateInputs();

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
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestConditionalExecution_IfStatement(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("ConditionalExecution_IfStatement.config");

            var inputs = CreateInputs(new NameValueCollection
            {
                ["ShouldLogin"] = "false"
            });

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
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestExecuteSpecificTarget(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("ExecuteSpecificTarget.config");
            var inputs = CreateInputs();

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
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestRedirect_301_FollowsRedirect(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("RedirectTest.config");
            var inputs = CreateInputs();

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
        [MemberData(nameof(GetWebAccessors))]
        public async Task TestRedirect_AfterPost_FollowsRedirect(string accessorType)
        {
            // Arrange
            var webAccessor = CreateWebAccessor(accessorType);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("RedirectAfterPostTest.config");
            var inputs = CreateInputs();

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

using System;
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

        [Fact]
        public async Task TestGetRequest_ExtractTitle()
        {
            // Arrange
            var client = _factory.CreateClient();
            var webAccessor = new HttpClientWebAccessor(client);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractTitle.config");

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Welcome to Test Shop", result.Outputs["title"]);
        }

        [Fact]
        public async Task TestGetRequest_ExtractMultipleValues()
        {
            // Arrange
            var client = _factory.CreateClient();
            var webAccessor = new HttpClientWebAccessor(client);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractMultipleValues.config");

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Contains("Value One", result.Outputs["values"]);
            Assert.Contains("Value Two", result.Outputs["values"]);
            Assert.Contains("Value Three", result.Outputs["values"]);
        }

        [Fact]
        public async Task TestGetRequest_ExtractAttribute()
        {
            // Arrange
            var client = _factory.CreateClient();
            var webAccessor = new HttpClientWebAccessor(client);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractAttribute.config");

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("/images/test.png", result.Outputs["imageSrc"]);
        }

        [Fact]
        public async Task TestGetRequest_ExtractWithRegex()
        {
            // Arrange
            var client = _factory.CreateClient();
            var webAccessor = new HttpClientWebAccessor(client);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExtractWithRegex.config");

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("98765", result.Outputs["orderId"]);
        }

        [Fact]
        public async Task TestGetRequest_ExpectedValueValidation_Success()
        {
            // Arrange
            var client = _factory.CreateClient();
            var webAccessor = new HttpClientWebAccessor(client);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExpectedValueValidation_Success.config");

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
        }

        [Fact]
        public async Task TestGetRequest_ExpectedValueValidation_Failure()
        {
            // Arrange
            var client = _factory.CreateClient();
            var webAccessor = new HttpClientWebAccessor(client);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("GetRequest_ExpectedValueValidation_Failure.config");

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.False(result.Ok);
            Assert.Contains("Expected", result.Message);
        }

        [Fact]
        public async Task TestPostRequest_FormSubmission()
        {
            // Arrange
            var client = _factory.CreateClient();
            var webAccessor = new HttpClientWebAccessor(client);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("PostRequest_FormSubmission.config");

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
        }

        [Fact]
        public async Task TestPostRequest_FormSubmissionWithOutputExtraction()
        {
            // Arrange
            var client = _factory.CreateClient();
            var webAccessor = new HttpClientWebAccessor(client);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("PostRequest_FormSubmissionWithOutputExtraction.config");

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Jane Smith", result.Outputs["customerName"]);
        }

        [Fact]
        public async Task TestLoginFlow_WithInputParameters()
        {
            // Arrange
            var client = _factory.CreateClient();
            var webAccessor = new HttpClientWebAccessor(client);
            var executor = new InterationExecutor(webAccessor);

            // This test focuses on input parameter substitution
            var config = LoadConfig("LoginFlow_WithInputParameters.config");

            var inputs = new NameValueCollection
            {
                ["BillingName"] = "Test User",
                ["Email"] = "test@example.com"
            };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Test User", result.Outputs["customerName"]);
            Assert.Equal("test@example.com", result.Outputs["customerEmail"]);
        }

        [Fact]
        public async Task TestMultiStepWorkflow_ShoppingCart()
        {
            // Arrange
            var client = _factory.CreateClient();
            var webAccessor = new HttpClientWebAccessor(client);
            var executor = new InterationExecutor(webAccessor);

            // This test focuses on multi-step workflow - note that outputs from later steps
            // may overwrite earlier outputs if they use the same context
            var config = LoadConfig("MultiStepWorkflow_ShoppingCart.config");

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            // Check that cart outputs are available (final step outputs)
            Assert.Equal("$25.00", result.Outputs["cartTotal"]);
            Assert.Equal("Medium", result.Outputs["itemSize"]); // Default cart item
        }

        [Fact]
        public async Task TestCallTarget_ReusableWorkflow()
        {
            // Arrange
            var client = _factory.CreateClient();
            var webAccessor = new HttpClientWebAccessor(client);
            var executor = new InterationExecutor(webAccessor);

            // This test focuses on the call target functionality
            var config = LoadConfig("CallTarget_ReusableWorkflow.config");

            // Act
            var result = await executor.ExecuteAsync(config);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Data Extraction Test", result.Outputs["title"]);
        }

        [Fact]
        public async Task TestConditionalExecution_IfStatement()
        {
            // Arrange
            var client = _factory.CreateClient();
            var webAccessor = new HttpClientWebAccessor(client);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("ConditionalExecution_IfStatement.config");

            var inputs = new NameValueCollection
            {
                ["ShouldLogin"] = "false"
            };

            // Act
            var result = await executor.ExecuteAsync(config, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Data Extraction Test", result.Outputs["title"]);
        }

        [Fact]
        public async Task TestExecuteSpecificTarget()
        {
            // Arrange
            var client = _factory.CreateClient();
            var webAccessor = new HttpClientWebAccessor(client);
            var executor = new InterationExecutor(webAccessor);
            var config = LoadConfig("ExecuteSpecificTarget.config");

            // Act - Execute specific target instead of default
            var result = await executor.ExecuteAsync(config, null, "Products");

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.Equal("Test Product", result.Outputs["productName"]);
            Assert.Null(result.Outputs["title"]); // Should not have executed Main target
        }
    }
}

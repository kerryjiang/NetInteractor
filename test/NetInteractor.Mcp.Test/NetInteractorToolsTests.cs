using System.Threading.Tasks;
using NetInteractor.Mcp;
using NetInteractor.Test.TestWebApp;
using Xunit;

namespace NetInteractor.Mcp.Test
{
    public class NetInteractorToolsTests : IClassFixture<TestWebApplicationFixture>
    {
        private readonly TestWebApplicationFactory _factory;
        private readonly string _baseUrl;

        public NetInteractorToolsTests(TestWebApplicationFixture fixture)
        {
            _factory = fixture.Factory;
            _baseUrl = fixture.Factory.ServerUrl;
        }

        [Fact]
        public async Task GetAsync_SimpleRequest_ReturnsHtml()
        {
            // Act
            var result = await NetInteractorTools.GetAsync($"{_baseUrl}/");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Html);
            Assert.Contains("Welcome to Test Shop", result.Html);
        }

        [Fact]
        public async Task GetAsync_WithXPath_ExtractsTitle()
        {
            // Act
            var result = await NetInteractorTools.GetAsync($"{_baseUrl}/", "//h1", "text()");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Welcome to Test Shop", result.ExtractedValue);
            Assert.Null(result.Html);
        }

        [Fact]
        public async Task GetAsync_WithXPath_ExtractsAttribute()
        {
            // Act
            var result = await NetInteractorTools.GetAsync($"{_baseUrl}/data", "//img", "src");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("/images/test.png", result.ExtractedValue);
        }

        [Fact]
        public async Task GetAsync_InvalidUrl_ReturnsFailed()
        {
            // Act
            var result = await NetInteractorTools.GetAsync("http://invalid-nonexistent-domain-123456789.com/");

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.Message);
        }

        [Fact]
        public async Task PostAsync_SimplePost_ReturnsSuccess()
        {
            // Arrange
            var formData = "billing_name=Test User,email=test@example.com,billing_address=123 Main St,billing_city=Seattle,billing_state=WA,billing_zip=98101,billing_country=USA,credit_card_number=4111111111111111,credit_card_month=12,credit_card_year=2027";

            // Act
            var result = await NetInteractorTools.PostAsync($"{_baseUrl}/checkout/submit", formData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Html);
            Assert.Contains("Test User", result.Html);
        }

        [Fact]
        public async Task PostAsync_WithXPath_ExtractsValue()
        {
            // Arrange
            var formData = "billing_name=Jane Doe,email=jane@example.com,billing_address=456 Oak St,billing_city=Portland,billing_state=OR,billing_zip=97201,billing_country=USA,credit_card_number=4111111111111111,credit_card_month=12,credit_card_year=2027";

            // Act
            var result = await NetInteractorTools.PostAsync($"{_baseUrl}/checkout/submit", formData, "//span[@class='customer-name']", "text()");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Jane Doe", result.ExtractedValue);
        }

        [Fact]
        public async Task ExecuteScriptAsync_SimpleGetScript_ExtractsTitle()
        {
            // Arrange
            var script = @"<InteractConfig defaultTarget='Main'>
                <target name='Main'>
                    <get url='$(BaseUrl)/'>
                        <output name='title' xpath='//h1' attr='text()' />
                    </get>
                </target>
            </InteractConfig>";

            // Act
            var result = await NetInteractorTools.ExecuteScriptAsync(script, $"BaseUrl={_baseUrl}");

            // Assert
            Assert.True(result.Success, result.Message);
            Assert.NotNull(result.Outputs);
            Assert.Equal("Welcome to Test Shop", result.Outputs["title"]);
        }

        [Fact]
        public async Task ExecuteScriptAsync_WithSpecificTarget_ExecutesTarget()
        {
            // Arrange
            var script = @"<InteractConfig defaultTarget='Main'>
                <target name='Main'>
                    <get url='$(BaseUrl)/'>
                        <output name='title' xpath='//h1' attr='text()' />
                    </get>
                </target>
                <target name='Products'>
                    <get url='$(BaseUrl)/products'>
                        <output name='productTitle' xpath='//h1' attr='text()' />
                    </get>
                </target>
            </InteractConfig>";

            // Act
            var result = await NetInteractorTools.ExecuteScriptAsync(script, $"BaseUrl={_baseUrl}", "Products");

            // Assert
            Assert.True(result.Success, result.Message);
            Assert.NotNull(result.Outputs);
            Assert.Equal("Products", result.Outputs["productTitle"]);
        }

        [Fact]
        public async Task ExecuteScriptAsync_InvalidScript_ReturnsFailed()
        {
            // Arrange
            var invalidScript = @"<Invalid></Script>";

            // Act
            var result = await NetInteractorTools.ExecuteScriptAsync(invalidScript);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.Message);
        }

        [Fact]
        public async Task ExecuteScriptAsync_NoTargetSpecified_ReturnsError()
        {
            // Arrange - Script without defaultTarget and no target parameter
            var script = @"<InteractConfig>
                <target name='Main'>
                    <get url='$(BaseUrl)/' />
                </target>
            </InteractConfig>";

            // Act
            var result = await NetInteractorTools.ExecuteScriptAsync(script, $"BaseUrl={_baseUrl}");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("target", result.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ExecuteScriptAsync_MultipleOutputs_ExtractsAllValues()
        {
            // Arrange
            var script = @"<InteractConfig defaultTarget='Main'>
                <target name='Main'>
                    <get url='$(BaseUrl)/data'>
                        <output name='title' xpath='//h1' attr='text()' />
                        <output name='imageSrc' xpath='//img' attr='src' />
                    </get>
                </target>
            </InteractConfig>";

            // Act
            var result = await NetInteractorTools.ExecuteScriptAsync(script, $"BaseUrl={_baseUrl}");

            // Assert
            Assert.True(result.Success, result.Message);
            Assert.NotNull(result.Outputs);
            Assert.Equal("Data Extraction Test", result.Outputs["title"]);
            Assert.Equal("/images/test.png", result.Outputs["imageSrc"]);
        }

        [Fact]
        public async Task ExecuteScriptAsync_WithNullInputs_ExecutesSuccessfully()
        {
            // Arrange
            var script = @"<InteractConfig defaultTarget='Main'>
                <target name='Main'>
                    <get url='" + _baseUrl + @"/'>
                        <output name='title' xpath='//h1' attr='text()' />
                    </get>
                </target>
            </InteractConfig>";

            // Act
            var result = await NetInteractorTools.ExecuteScriptAsync(script, null);

            // Assert
            Assert.True(result.Success, result.Message);
            Assert.NotNull(result.Outputs);
            Assert.Equal("Welcome to Test Shop", result.Outputs["title"]);
        }
    }

    /// <summary>
    /// Shared test fixture that provides a single TestWebApplicationFactory instance for all tests.
    /// </summary>
    public class TestWebApplicationFixture : System.IDisposable
    {
        public TestWebApplicationFactory Factory { get; }

        public TestWebApplicationFixture()
        {
            Factory = new TestWebApplicationFactory(ServerMode.Kestrel);
        }

        public void Dispose()
        {
            Factory?.Dispose();
        }
    }
}

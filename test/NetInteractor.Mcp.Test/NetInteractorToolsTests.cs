using System.Threading.Tasks;
using NetInteractor.Mcp;
using NetInteractor.Test.TestWebApp;
using NetInteractor.WebAccessors;
using Xunit;

namespace NetInteractor.Mcp.Test
{
    public class NetInteractorToolTests : IClassFixture<TestWebApplicationFixture>
    {
        private readonly TestWebApplicationFactory _factory;
        private readonly string _baseUrl;
        private readonly NetInteractorTool _tool;

        public NetInteractorToolTests(TestWebApplicationFixture fixture)
        {
            _factory = fixture.Factory;
            _baseUrl = fixture.Factory.ServerUrl;
            // Use HttpClientWebAccessor for tests since Playwright requires browser installation
            _tool = new NetInteractorTool(new HttpClientWebAccessor());
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
            var result = await _tool.ExecuteScriptAsync(script, $"BaseUrl={_baseUrl}");

            // Assert
            Assert.True(result.Ok, result.Message);
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
            var result = await _tool.ExecuteScriptAsync(script, $"BaseUrl={_baseUrl}", "Products");

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.NotNull(result.Outputs);
            Assert.Equal("Products", result.Outputs["productTitle"]);
        }

        [Fact]
        public async Task ExecuteScriptAsync_InvalidScript_ReturnsFailed()
        {
            // Arrange
            var invalidScript = @"<Invalid></Script>";

            // Act
            var result = await _tool.ExecuteScriptAsync(invalidScript);

            // Assert
            Assert.False(result.Ok);
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
            var result = await _tool.ExecuteScriptAsync(script, $"BaseUrl={_baseUrl}");

            // Assert
            Assert.False(result.Ok);
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
            var result = await _tool.ExecuteScriptAsync(script, $"BaseUrl={_baseUrl}");

            // Assert
            Assert.True(result.Ok, result.Message);
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
            var result = await _tool.ExecuteScriptAsync(script, null);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.NotNull(result.Outputs);
            Assert.Equal("Welcome to Test Shop", result.Outputs["title"]);
        }

        [Fact]
        public void GetInputMetadata_ReturnsExpectedParameters()
        {
            // Act
            var metadata = NetInteractorTool.GetInputMetadata();

            // Assert
            Assert.NotNull(metadata);
            Assert.True(metadata.ContainsKey("script"));
            Assert.True(metadata.ContainsKey("inputs"));
            Assert.True(metadata.ContainsKey("target"));
            
            Assert.True(metadata["script"].Required);
            Assert.False(metadata["inputs"].Required);
            Assert.False(metadata["target"].Required);
        }

        [Fact]
        public void GetOutputMetadata_ReturnsExpectedParameters()
        {
            // Act
            var metadata = NetInteractorTool.GetOutputMetadata();

            // Assert
            Assert.NotNull(metadata);
            Assert.True(metadata.ContainsKey("Ok"));
            Assert.True(metadata.ContainsKey("Message"));
            Assert.True(metadata.ContainsKey("Outputs"));
            
            Assert.Equal("boolean", metadata["Ok"].Type);
            Assert.Equal("string", metadata["Message"].Type);
            Assert.Equal("NameValueCollection", metadata["Outputs"].Type);
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

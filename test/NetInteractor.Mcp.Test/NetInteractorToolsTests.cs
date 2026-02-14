using System.Collections.Specialized;
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
        public async Task ExecuteScriptInternalAsync_SimpleGetScript_ExtractsTitle()
        {
            // Arrange
            var script = $@"<InteractConfig defaultTarget='Main'>
                <target name='Main'>
                    <get url='{_baseUrl}/'>
                        <output name='title' xpath='//h1' attr='text()' />
                    </get>
                </target>
            </InteractConfig>";

            // Act
            var result = await _tool.ExecuteScriptInternalAsync(script);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.NotNull(result.Outputs);
            Assert.Equal("Welcome to Test Shop", result.Outputs["title"]);
        }

        [Fact]
        public async Task ExecuteScriptInternalAsync_WithInputs_ExtractsTitle()
        {
            // Arrange
            var script = @"<InteractConfig defaultTarget='Main'>
                <target name='Main'>
                    <get url='$(BaseUrl)/'>
                        <output name='title' xpath='//h1' attr='text()' />
                    </get>
                </target>
            </InteractConfig>";

            var inputs = new NameValueCollection { { "BaseUrl", _baseUrl } };

            // Act
            var result = await _tool.ExecuteScriptInternalAsync(script, inputs);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.NotNull(result.Outputs);
            Assert.Equal("Welcome to Test Shop", result.Outputs["title"]);
        }

        [Fact]
        public async Task ExecuteScriptInternalAsync_WithSpecificTarget_ExecutesTarget()
        {
            // Arrange
            var script = $@"<InteractConfig defaultTarget='Main'>
                <target name='Main'>
                    <get url='{_baseUrl}/'>
                        <output name='title' xpath='//h1' attr='text()' />
                    </get>
                </target>
                <target name='Products'>
                    <get url='{_baseUrl}/products'>
                        <output name='productTitle' xpath='//h1' attr='text()' />
                    </get>
                </target>
            </InteractConfig>";

            // Act
            var result = await _tool.ExecuteScriptInternalAsync(script, null, "Products");

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.NotNull(result.Outputs);
            Assert.Equal("Products", result.Outputs["productTitle"]);
        }

        [Fact]
        public async Task ExecuteScriptInternalAsync_InvalidScript_ReturnsError()
        {
            // Arrange
            var invalidScript = @"<Invalid></Script>";

            // Act
            var result = await _tool.ExecuteScriptInternalAsync(invalidScript);

            // Assert
            Assert.False(result.Ok);
            Assert.NotNull(result.Message);
        }

        [Fact]
        public async Task ExecuteScriptInternalAsync_MultipleOutputs_ExtractsAllValues()
        {
            // Arrange
            var script = $@"<InteractConfig defaultTarget='Main'>
                <target name='Main'>
                    <get url='{_baseUrl}/data'>
                        <output name='title' xpath='//h1' attr='text()' />
                        <output name='imageSrc' xpath='//img' attr='src' />
                    </get>
                </target>
            </InteractConfig>";

            // Act
            var result = await _tool.ExecuteScriptInternalAsync(script);

            // Assert
            Assert.True(result.Ok, result.Message);
            Assert.NotNull(result.Outputs);
            Assert.Equal("Data Extraction Test", result.Outputs["title"]);
            Assert.Equal("/images/test.png", result.Outputs["imageSrc"]);
        }

        [Fact]
        public void ProtocolTool_ReturnsValidTool()
        {
            // Act
            var protocolTool = _tool.ProtocolTool;

            // Assert
            Assert.NotNull(protocolTool);
            Assert.Equal("netinteractor_execute_script", protocolTool.Name);
            Assert.NotNull(protocolTool.Description);
            Assert.NotNull(protocolTool.OutputSchema);
        }

        [Fact]
        public void Metadata_ReturnsEmptyList()
        {
            // Act
            var metadata = _tool.Metadata;

            // Assert - Metadata returns empty list, output schema is on ProtocolTool.OutputSchema
            Assert.NotNull(metadata);
            Assert.Empty(metadata);
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
